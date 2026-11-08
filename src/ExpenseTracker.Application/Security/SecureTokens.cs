using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseTracker.Application.Security;

/// <summary>Opaque secrets (refresh tokens, API keys) and their at-rest hashes.</summary>
public static class SecureTokens
{
    private const int DefaultEntropyBytes = 32;

    /// <summary>URL-safe random string with <paramref name="bytes"/> bytes of entropy.</summary>
    public static string Generate(int bytes = DefaultEntropyBytes) => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(bytes));

    /// <summary>
    /// SHA-256 (hex). A fast hash is right here: unlike passwords, these secrets are long random values,
    /// so they can't be brute-forced, and an indexed hash lookup keeps authentication cheap.
    /// </summary>
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
