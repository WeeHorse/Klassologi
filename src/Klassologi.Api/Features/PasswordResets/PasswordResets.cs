using Dapper;
using Klassologi.Api.Auth;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Classes;
using Microsoft.AspNetCore.Identity;

namespace Klassologi.Api.Features.PasswordResets;

/// <summary>Återställa lösenord (steg 3). Se Specifications/UseCases.md.</summary>
public static class PasswordResets
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public static void MapPasswordResetEndpoints(this WebApplication app)
    {
        app.MapPost("/api/classes/{classId:guid}/members/{userId:guid}/password-reset", Create).RequireAuthorization();
        app.MapPost("/api/password-resets/complete", Complete).RequireRateLimiting(AuthEndpoints.RateLimitPolicy);
    }

    public sealed record CompleteRequest(string? Token, string? NewPassword);

    sealed class TargetRow { public string Role { get; set; } = ""; public bool IsAdministrator { get; set; } }
    sealed class ResetRow { public Guid Id { get; set; } public Guid UserId { get; set; } }

    static async Task<IResult> Create(Guid classId, Guid userId, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } actorId) return Results.Unauthorized();

        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, actorId);
        if (access.Deny(access.CanManageMembers) is { } denied) return denied;

        var target = await connection.QuerySingleOrDefaultAsync<TargetRow>(
            """
            SELECT m.Role, u.IsAdministrator
            FROM class_memberships m JOIN users u ON u.Id = m.UserId
            WHERE m.ClassId = @classId AND m.UserId = @userId
            """, new { classId, userId });
        if (target is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Användaren är inte medlem i klassen.");

        // Den som skapar länken kan använda den själv. Lärare får därför inte återställa för lärare eller administratörer.
        if (!access.IsAdministrator && (target.Role == nameof(ClassRole.Teacher) || target.IsAdministrator))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var (token, hash) = OneTimeLinks.NewToken();
        var expiresAt = DateTime.UtcNow.Add(Lifetime);
        await connection.ExecuteAsync(
            """
            INSERT INTO password_resets (Id, TokenHash, UserId, CreatedBy, ExpiresAt)
            VALUES (@id, @hash, @userId, @actorId, @expiresAt)
            """,
            new { id = Guid.CreateVersion7(), hash, userId, actorId, expiresAt });

        return Results.Json(OneTimeLinks.PasswordReset(token, expiresAt), statusCode: StatusCodes.Status201Created);
    }

    static async Task<IResult> Complete(
        CompleteRequest request, Db db, UserManager<AppUser> users, SignInManager<AppUser> signIn, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.Token)) return NotValid();

        await using var connection = await db.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var reset = await connection.QuerySingleOrDefaultAsync<ResetRow>(
            """
            SELECT Id, UserId FROM password_resets
            WHERE TokenHash = @hash AND UsedAt IS NULL AND ExpiresAt > UTC_TIMESTAMP(6)
            FOR UPDATE
            """, new { hash = OneTimeLinks.Hash(request.Token) }, transaction);
        if (reset is null) return NotValid();

        var user = await users.FindByIdAsync(reset.UserId.ToString());
        if (user is null) return NotValid();

        if (await Accounts.PasswordErrorAsync(users, user, request.NewPassword) is { } error)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = [error] });

        // Nytt lösenord, ny säkerhetsstämpel (alla sessioner blir ogiltiga) och spärren hävs.
        user.PasswordHash = users.PasswordHasher.HashPassword(user, request.NewPassword!);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await connection.ExecuteAsync(
            """
            UPDATE users SET PasswordHash = @PasswordHash, SecurityStamp = @SecurityStamp,
                AccessFailedCount = 0, LockoutEnd = NULL
            WHERE Id = @Id
            """, user, transaction);
        await connection.ExecuteAsync(
            "UPDATE password_resets SET UsedAt = UTC_TIMESTAMP(6) WHERE Id = @id", new { id = reset.Id }, transaction);
        await transaction.CommitAsync(ct);

        await signIn.SignInAsync(user, isPersistent: true);
        return Results.NoContent();
    }

    static IResult NotValid() => Results.Problem(
        statusCode: StatusCodes.Status404NotFound, title: "Länken gäller inte längre. Be din lärare om en ny.");
}
