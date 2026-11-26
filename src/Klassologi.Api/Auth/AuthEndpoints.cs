using Dapper;
using Klassologi.Api.Data;
using Microsoft.AspNetCore.Identity;

namespace Klassologi.Api.Auth;

/// <summary>UC-05 Logga in och logga ut. Se Specifications/UseCases.md.</summary>
public static class AuthEndpoints
{
    public const string RateLimitPolicy = "auth";

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth");
        auth.MapPost("/login", Login).RequireRateLimiting(RateLimitPolicy);
        auth.MapPost("/logout", Logout).RequireAuthorization();
        auth.MapGet("/me", Me).RequireAuthorization();
    }

    public sealed record LoginRequest(string? Email, string? Password);
    public sealed record MeResponse(Guid Id, string DisplayName, string? Email, bool IsAdministrator);

    public static IResult Problem(int status, string title, string code) =>
        Results.Problem(statusCode: status, title: title, extensions: new Dictionary<string, object?> { ["code"] = code });

    static async Task<IResult> Login(LoginRequest request, SignInManager<AppUser> signIn)
    {
        var result = await signIn.PasswordSignInAsync(
            request.Email ?? "", request.Password ?? "", isPersistent: true, lockoutOnFailure: true);

        if (result.Succeeded) return Results.NoContent();
        if (result.IsLockedOut)
            return Problem(StatusCodes.Status401Unauthorized, "För många misslyckade försök. Försök igen om 15 minuter.", "locked_out");
        return Problem(StatusCodes.Status401Unauthorized, "Fel e-postadress eller lösenord.", "invalid_credentials");
    }

    static async Task<IResult> Logout(HttpContext http, UserManager<AppUser> users, SignInManager<AppUser> signIn)
    {
        // En ny säkerhetsstämpel gör alla sessioner för kontot ogiltiga, inte bara den här.
        if (CurrentUser.Id(http.User) is { } id && await users.FindByIdAsync(id.ToString()) is { } user)
            await users.UpdateSecurityStampAsync(user);
        await signIn.SignOutAsync();
        return Results.NoContent();
    }

    static async Task<IResult> Me(HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } id) return Results.Unauthorized();
        await using var connection = await db.OpenAsync(ct);
        var me = await connection.QuerySingleOrDefaultAsync<MeResponse>(
            "SELECT Id, DisplayName, Email, IsAdministrator FROM users WHERE Id = @id", new { id });
        return me is null ? Results.Unauthorized() : Results.Ok(me);
    }
}
