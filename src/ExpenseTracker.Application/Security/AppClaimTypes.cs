namespace ExpenseTracker.Application.Security;

/// <summary>
/// Claim types used in access tokens and authenticated principals. Short JWT-registered names are
/// used throughout (inbound claim mapping is turned off), so tokens and principals read the same.
/// </summary>
public static class AppClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
    /// <summary>The user's security stamp when the token was issued; a changed stamp invalidates the token.</summary>
    public const string SecurityStamp = "sstamp";
    /// <summary>Authentication method reference: <c>pwd</c> (password → JWT) or <c>api_key</c>.</summary>
    public const string AuthenticationMethod = "amr";
    public const string ApiKeyId = "api_key_id";

    /// <summary>Claim types that are derived by the system and can never be set as user claims.</summary>
    public static IReadOnlySet<string> Reserved { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Subject, Name, Email, Role, SecurityStamp, AuthenticationMethod, ApiKeyId,
        "jti", "iat", "nbf", "exp", "iss", "aud",
    };
}
