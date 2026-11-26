using System.Data;
using Dapper;
using Klassologi.Api.Auth;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Classes;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace Klassologi.Api.Features.Invitations;

/// <summary>
/// UC-02 Bjuda in (enkel variant), UC-04 Aktivera konto via inbjudan och administratörsinbjudan (Q15).
/// Se Specifications/UseCases.md.
/// </summary>
public static class Invitations
{
    public static readonly TimeSpan ClassInvitationLifetime = TimeSpan.FromDays(7);
    public static readonly TimeSpan AdministratorInvitationLifetime = TimeSpan.FromHours(24);

    const string LinkNotValid = "Länken gäller inte längre. Be den som bjöd in dig om en ny.";

    public static void MapInvitationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/classes/{classId:guid}/invitations", CreateClassInvitation).RequireAuthorization();
        app.MapPost("/api/invitations/inspect", Inspect).RequireRateLimiting(AuthEndpoints.RateLimitPolicy);
        app.MapPost("/api/invitations/accept", Accept).RequireRateLimiting(AuthEndpoints.RateLimitPolicy);
    }

    public sealed record CreateInvitationRequest(ClassRole Role);
    public sealed record TokenRequest(string? Token);
    public sealed record AcceptRequest(string? Token, string? DisplayName, string? Email, string? Password);
    public sealed record InspectResponse(string? ClassName, ClassRole? Role, bool IsAdministrator);
    public sealed record AcceptResponse(Guid? ClassId);

    sealed class InvitationRow
    {
        public Guid Id { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string? Role { get; set; }
        public bool IsAdministrator { get; set; }
    }

    /// <summary>Skapar en inbjudan till en klass. Anroparen ansvarar för att behörigheten är kontrollerad.</summary>
    public static async Task<OneTimeLinks.Link> CreateClassInvitationAsync(
        IDbConnection connection, IDbTransaction? transaction, Guid classId, ClassRole role, Guid createdBy)
    {
        var (token, hash) = OneTimeLinks.NewToken();
        var expiresAt = DateTime.UtcNow.Add(ClassInvitationLifetime);
        await connection.ExecuteAsync(
            """
            INSERT INTO invitations (Id, TokenHash, ClassId, Role, IsAdministrator, CreatedBy, ExpiresAt)
            VALUES (@id, @hash, @classId, @role, FALSE, @createdBy, @expiresAt)
            """,
            new { id = Guid.CreateVersion7(), hash, classId, role = role.ToString(), createdBy, expiresAt }, transaction);
        return OneTimeLinks.Activation(token, expiresAt);
    }

    /// <summary>Q15: används av kommandot create-admin-invitation, aldrig av en endpoint.</summary>
    public static async Task<OneTimeLinks.Link> CreateAdministratorInvitationAsync(Db db)
    {
        var (token, hash) = OneTimeLinks.NewToken();
        var expiresAt = DateTime.UtcNow.Add(AdministratorInvitationLifetime);
        await using var connection = await db.OpenAsync();
        await connection.ExecuteAsync(
            """
            INSERT INTO invitations (Id, TokenHash, ClassId, Role, IsAdministrator, CreatedBy, ExpiresAt)
            VALUES (@id, @hash, NULL, NULL, TRUE, NULL, @expiresAt)
            """,
            new { id = Guid.CreateVersion7(), hash, expiresAt });
        return OneTimeLinks.Activation(token, expiresAt);
    }

    // UC-02
    static async Task<IResult> CreateClassInvitation(
        Guid classId, CreateInvitationRequest request, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } actorId) return Results.Unauthorized();
        if (!Enum.IsDefined(request.Role)) return Results.BadRequest();

        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, actorId);
        if (access.Deny(access.CanManageMembers) is { } denied) return denied;

        var link = await CreateClassInvitationAsync(connection, null, classId, request.Role, actorId);
        return Results.Json(link, statusCode: StatusCodes.Status201Created);
    }

    static async Task<InvitationRow?> FindValidAsync(IDbConnection connection, string? token, IDbTransaction? transaction, bool forUpdate)
    {
        if (string.IsNullOrEmpty(token)) return null;
        return await connection.QuerySingleOrDefaultAsync<InvitationRow>(
            $"""
            SELECT i.Id, i.ClassId, c.Name AS ClassName, i.Role, i.IsAdministrator
            FROM invitations i
            LEFT JOIN classes c ON c.Id = i.ClassId
            WHERE i.TokenHash = @hash AND i.UsedAt IS NULL AND i.ExpiresAt > UTC_TIMESTAMP(6)
            {(forUpdate ? "FOR UPDATE OF i" : "")}
            """,
            new { hash = OneTimeLinks.Hash(token) }, transaction);
    }

    static IResult NotValid() => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: LinkNotValid);

    static async Task<IResult> Inspect(TokenRequest request, Db db, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        var invitation = await FindValidAsync(connection, request.Token, null, forUpdate: false);
        if (invitation is null) return NotValid();
        return Results.Ok(new InspectResponse(
            invitation.ClassName, invitation.Role is null ? null : Enum.Parse<ClassRole>(invitation.Role), invitation.IsAdministrator));
    }

    // UC-04
    static async Task<IResult> Accept(
        AcceptRequest request, HttpContext http, Db db, UserManager<AppUser> users, SignInManager<AppUser> signIn, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        // Låset gör att samma inbjudan bara kan användas en gång, även vid samtidiga anrop.
        var invitation = await FindValidAsync(connection, request.Token, transaction, forUpdate: true);
        if (invitation is null) return NotValid();

        // A2 och A5: inloggad användare använder sitt befintliga konto.
        if (CurrentUser.Id(http.User) is { } existingUserId)
        {
            await GrantAsync(connection, transaction, invitation, existingUserId);
            await transaction.CommitAsync(ct);
            return Results.Ok(new AcceptResponse(invitation.ClassId));
        }

        // Huvudflödet: nytt konto.
        var user = new AppUser
        {
            Id = Guid.CreateVersion7(),
            DisplayName = request.DisplayName?.Trim() ?? "",
            Email = request.Email?.Trim(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            IsAdministrator = invitation.IsAdministrator,
        };
        var errors = new Dictionary<string, string[]>();
        if (Accounts.DisplayNameError(request.DisplayName) is { } nameError) errors["displayName"] = [nameError];
        if (Accounts.EmailError(user.Email) is { } emailError) errors["email"] = [emailError];
        if (await Accounts.PasswordErrorAsync(users, user, request.Password) is { } passwordError) errors["password"] = [passwordError];
        if (errors.Count > 0) return Results.ValidationProblem(errors);

        user.NormalizedEmail = users.NormalizeEmail(user.Email);
        user.PasswordHash = users.PasswordHasher.HashPassword(user, request.Password!);

        try
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO users (Id, DisplayName, Email, NormalizedEmail, PasswordHash, SecurityStamp, IsAdministrator)
                VALUES (@Id, @DisplayName, @Email, @NormalizedEmail, @PasswordHash, @SecurityStamp, FALSE)
                """, user, transaction);
        }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return AuthEndpoints.Problem(StatusCodes.Status409Conflict,
                "Det finns redan ett konto med den e-postadressen. Logga in för att gå med i klassen.", "email_taken");
        }

        await GrantAsync(connection, transaction, invitation, user.Id);
        await transaction.CommitAsync(ct);

        await signIn.SignInAsync(user, isPersistent: true);
        return Results.Json(new AcceptResponse(invitation.ClassId), statusCode: StatusCodes.Status201Created);
    }

    /// <summary>Ger behörigheten från inbjudan och förbrukar den. En befintlig medlem behåller sin roll.</summary>
    static async Task GrantAsync(IDbConnection connection, IDbTransaction transaction, InvitationRow invitation, Guid userId)
    {
        if (invitation.IsAdministrator)
            await connection.ExecuteAsync(
                "UPDATE users SET IsAdministrator = TRUE WHERE Id = @userId", new { userId }, transaction);
        else
            await connection.ExecuteAsync(
                """
                INSERT INTO class_memberships (ClassId, UserId, Role) VALUES (@classId, @userId, @role)
                ON DUPLICATE KEY UPDATE UserId = UserId
                """,
                new { classId = invitation.ClassId, userId, role = invitation.Role }, transaction);

        await connection.ExecuteAsync(
            "UPDATE invitations SET UsedAt = UTC_TIMESTAMP(6), UsedBy = @userId WHERE Id = @id",
            new { id = invitation.Id, userId }, transaction);
    }
}
