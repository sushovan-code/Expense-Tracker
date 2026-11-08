using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Api.Auth;

/// <summary>Access-token settings, bound from <c>Authentication:Jwt</c> and validated at startup.</summary>
public sealed class JwtOptions
{
    public const string Section = "Authentication:Jwt";

    [Required] public string Issuer { get; init; } = "";

    [Required] public string Audience { get; init; } = "";

    /// <summary>
    /// HMAC-SHA256 key of at least 32 characters. Never commit a real key: supply it through user secrets,
    /// a vault or the <c>Authentication__Jwt__SigningKey</c> environment variable.
    /// </summary>
    [Required, MinLength(32)] public string SigningKey { get; init; } = "";

    /// <summary>Kept short: access tokens can't be revoked individually, only outlived.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
