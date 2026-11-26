using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Klassologi.Api.Auth;

/// <summary>Regler för kontouppgifter (steg 3). Felmeddelanden på klarspråk, per fält.</summary>
public static class Accounts
{
    public const int MinPasswordLength = 12;
    public const int MaxPasswordLength = 128;
    public const int MaxDisplayNameLength = 100;
    public const int MaxEmailLength = 256;

    public static async Task<string?> PasswordErrorAsync(UserManager<AppUser> users, AppUser user, string? password)
    {
        if (password is null || password.Length is < MinPasswordLength or > MaxPasswordLength)
            return $"Lösenordet måste vara {MinPasswordLength}–{MaxPasswordLength} tecken.";
        foreach (var validator in users.PasswordValidators)
            if (!(await validator.ValidateAsync(users, user, password)).Succeeded)
                return "Lösenordet uppfyller inte kraven.";
        return null;
    }

    public static string? DisplayNameError(string? displayName) =>
        displayName?.Trim().Length is >= 1 and <= MaxDisplayNameLength ? null : $"Namnet måste vara 1–{MaxDisplayNameLength} tecken.";

    public static string? EmailError(string? email) =>
        email is { Length: <= MaxEmailLength } && new EmailAddressAttribute().IsValid(email) ? null : "Ange en giltig e-postadress.";
}
