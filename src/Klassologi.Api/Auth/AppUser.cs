namespace Klassologi.Api.Auth;

/// <summary>En användare som ASP.NET Core Identity ser den. E-postadressen är inloggningsnamn.</summary>
public sealed class AppUser
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = "";
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string? PasswordHash { get; set; }
    public string? SecurityStamp { get; set; }
    /// <summary>UTC.</summary>
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public bool IsAdministrator { get; set; }
}
