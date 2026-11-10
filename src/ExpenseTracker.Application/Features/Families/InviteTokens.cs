using System.Security.Cryptography;
using System.Text;

namespace ExpenseTracker.Application.Features.Families;

/// <summary>Random invite secrets; only their SHA-256 hash is ever stored.</summary>
internal static class InviteTokens
{
    private const int SecretBytes = 32;

    public static string NewToken() => Base64Url(RandomNumberGenerator.GetBytes(SecretBytes));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
