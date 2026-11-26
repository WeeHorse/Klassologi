using Dapper;
using Klassologi.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace Klassologi.Api.Auth;

/// <summary>
/// Lagring för ASP.NET Core Identity med Dapper. Identity sköter hashning av lösenord, säkerhetsstämplar och
/// spärr; här finns bara läsning och skrivning av tabellen users. E-postadressen används som användarnamn.
/// </summary>
public sealed class UserStore(Db db) :
    IUserPasswordStore<AppUser>,
    IUserEmailStore<AppUser>,
    IUserSecurityStampStore<AppUser>,
    IUserLockoutStore<AppUser>
{
    const string Columns =
        "Id, DisplayName, Email, NormalizedEmail, PasswordHash, SecurityStamp, LockoutEnd, AccessFailedCount, IsAdministrator";

    async Task<AppUser?> FindAsync(string where, object parameters, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<AppUser>(
            new CommandDefinition($"SELECT {Columns} FROM users WHERE {where}", parameters, cancellationToken: ct));
    }

    public async Task<IdentityResult> CreateAsync(AppUser user, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO users (Id, DisplayName, Email, NormalizedEmail, PasswordHash, SecurityStamp, IsAdministrator)
            VALUES (@Id, @DisplayName, @Email, @NormalizedEmail, @PasswordHash, @SecurityStamp, @IsAdministrator)
            """, user, cancellationToken: ct));
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken ct)
    {
        // Administratörsbehörighet ändras aldrig via Identity.
        await using var connection = await db.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE users SET DisplayName = @DisplayName, Email = @Email, NormalizedEmail = @NormalizedEmail,
                PasswordHash = @PasswordHash, SecurityStamp = @SecurityStamp, LockoutEnd = @LockoutEnd,
                AccessFailedCount = @AccessFailedCount
            WHERE Id = @Id
            """, user, cancellationToken: ct));
        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken ct) =>
        throw new NotSupportedException("Konton tas inte bort via Identity.");

    public Task<AppUser?> FindByIdAsync(string userId, CancellationToken ct) =>
        Guid.TryParse(userId, out var id) ? FindAsync("Id = @id", new { id }, ct) : Task.FromResult<AppUser?>(null);

    public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct) =>
        FindByEmailAsync(normalizedUserName, ct);

    public Task<AppUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct) =>
        FindAsync("NormalizedEmail = @normalizedEmail", new { normalizedEmail }, ct);

    public Task<string> GetUserIdAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.Id.ToString());
    public Task<string?> GetUserNameAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.Email);
    public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken ct) { user.Email = userName; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken ct) { user.NormalizedEmail = normalizedName; return Task.CompletedTask; }

    public Task<string?> GetPasswordHashAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash);
    public Task SetPasswordHashAsync(AppUser user, string? passwordHash, CancellationToken ct) { user.PasswordHash = passwordHash; return Task.CompletedTask; }
    public Task<bool> HasPasswordAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash is not null);

    public Task<string?> GetEmailAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.Email);
    public Task SetEmailAsync(AppUser user, string? email, CancellationToken ct) { user.Email = email; return Task.CompletedTask; }
    public Task<string?> GetNormalizedEmailAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedEmailAsync(AppUser user, string? normalizedEmail, CancellationToken ct) { user.NormalizedEmail = normalizedEmail; return Task.CompletedTask; }
    // Ingen e-post skickas i MVP:n (Q14), så adresser bekräftas inte.
    public Task<bool> GetEmailConfirmedAsync(AppUser user, CancellationToken ct) => Task.FromResult(true);
    public Task SetEmailConfirmedAsync(AppUser user, bool confirmed, CancellationToken ct) => Task.CompletedTask;

    public Task<string?> GetSecurityStampAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.SecurityStamp);
    public Task SetSecurityStampAsync(AppUser user, string stamp, CancellationToken ct) { user.SecurityStamp = stamp; return Task.CompletedTask; }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(AppUser user, CancellationToken ct) =>
        Task.FromResult(user.LockoutEnd is { } end ? new DateTimeOffset(DateTime.SpecifyKind(end, DateTimeKind.Utc)) : (DateTimeOffset?)null);
    public Task SetLockoutEndDateAsync(AppUser user, DateTimeOffset? lockoutEnd, CancellationToken ct) { user.LockoutEnd = lockoutEnd?.UtcDateTime; return Task.CompletedTask; }
    public Task<int> IncrementAccessFailedCountAsync(AppUser user, CancellationToken ct) => Task.FromResult(++user.AccessFailedCount);
    public Task ResetAccessFailedCountAsync(AppUser user, CancellationToken ct) { user.AccessFailedCount = 0; return Task.CompletedTask; }
    public Task<int> GetAccessFailedCountAsync(AppUser user, CancellationToken ct) => Task.FromResult(user.AccessFailedCount);
    // Spärr gäller alla konton.
    public Task<bool> GetLockoutEnabledAsync(AppUser user, CancellationToken ct) => Task.FromResult(true);
    public Task SetLockoutEnabledAsync(AppUser user, bool enabled, CancellationToken ct) => Task.CompletedTask;

    public void Dispose() { }
}
