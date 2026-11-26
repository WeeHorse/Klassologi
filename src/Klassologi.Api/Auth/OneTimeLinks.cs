using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Klassologi.Api.Auth;

/// <summary>
/// Engångslänkar för inbjudan och återställning av lösenord. Token har 256 bitar slump och lagras bara som
/// SHA-256-hash. Token läggs i länkens fragment (#), som webbläsaren inte skickar till servern.
/// </summary>
public static class OneTimeLinks
{
    public sealed record Link(string Url, DateTime ExpiresAt);

    public static (string Token, byte[] Hash) NewToken()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        return (token, Hash(token));
    }

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));

    public static Link Activation(string token, DateTime expiresAt) => new($"/aktivera#{token}", expiresAt);
    public static Link PasswordReset(string token, DateTime expiresAt) => new($"/aterstall#{token}", expiresAt);
}
