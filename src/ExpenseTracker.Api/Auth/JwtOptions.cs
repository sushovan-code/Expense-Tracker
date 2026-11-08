using System.ComponentModel.DataAnnotations;
using System.Text;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
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

/// <summary>Issues the API's access tokens; the same <see cref="JwtOptions"/> validate them.</summary>
internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedAccessToken Issue(AccessTokenSubject subject)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow();
        var expiresAt = now + jwt.AccessTokenLifetime;

        // Identity only. Roles and permissions are resolved per request on the server, so a token
        // never carries grants that have since been withdrawn.
        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [AppClaimTypes.Subject] = subject.UserId.ToString(),
                [AppClaimTypes.Name] = subject.DisplayName,
                [AppClaimTypes.Email] = subject.Email,
                [AppClaimTypes.SecurityStamp] = subject.SecurityStamp,
                [AppClaimTypes.AuthenticationMethod] = new[] { "pwd" },
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        });

        return new IssuedAccessToken(token, expiresAt);
    }
}
