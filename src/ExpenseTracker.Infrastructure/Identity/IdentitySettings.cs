using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Identity store settings, bound from the <c>Identity</c> section.</summary>
public sealed class IdentitySettings
{
    public const string Section = "Identity";

    /// <summary>How long a refresh token (and so an idle session) lives; each refresh starts a new period.</summary>
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    public IdentitySeedSettings Seed { get; init; } = new();
}

/// <summary>
/// Startup seeding. Roles and their default permissions are always seeded. The bootstrap SuperAdmin is
/// created only when <see cref="AdminEmail"/> and <see cref="AdminPassword"/> are set: supply the password
/// through user secrets or an environment variable (Identity__Seed__AdminPassword), never a committed file
/// outside Development.
/// </summary>
public sealed class IdentitySeedSettings
{
    public string? AdminEmail { get; init; }
    public string? AdminPassword { get; init; }
    public string AdminDisplayName { get; init; } = "Administrator";

    /// <summary>Development only: also create one demo user per lower role (admin@, user@, reader@ on the admin's domain).</summary>
    public bool DemoUsers { get; init; }
    public string? DemoPassword { get; init; }
}

/// <summary>Opaque secrets (refresh tokens, API keys) and their at-rest hashes.</summary>
internal static class SecureTokens
{
    /// <summary>URL-safe random string with <paramref name="bytes"/> bytes of entropy.</summary>
    public static string Generate(int bytes = 32) => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(bytes));

    /// <summary>
    /// SHA-256 (hex). A fast hash is right here: unlike passwords, these secrets are long random values,
    /// so they can't be brute-forced, and an indexed hash lookup keeps authentication cheap.
    /// </summary>
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
