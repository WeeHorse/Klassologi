using System.Security.Claims;

namespace Klassologi.Api.Auth;

/// <summary>
/// Den inloggade användarens id, läst från inloggningens "sub"-claim. Vilken inloggningslösning som sätter
/// claimet avgörs i steg 3 (Q2). Allt om behörighet läses därefter från databasen, aldrig från klienten.
/// </summary>
public static class CurrentUser
{
    public static Guid? Id(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true && Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : null;
}
