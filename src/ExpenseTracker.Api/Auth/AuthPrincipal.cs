using System.Security.Claims;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Auth;

internal static class AuthSchemes
{
    /// <summary>Default scheme: forwards to <see cref="ApiKey"/> when an <c>x-api-key</c> header is present, else to <see cref="Bearer"/>.</summary>
    public const string Default = "JwtOrApiKey";
    public const string Bearer = "Bearer";
    public const string ApiKey = "ApiKey";
    public const string ApiKeyHeader = "x-api-key";
}

internal static class AuthPolicies
{
    /// <summary>Signed in with a role of Admin or higher.</summary>
    public const string AdminOrAbove = nameof(AdminOrAbove);

    /// <summary>Signed in interactively (JWT). API keys can't manage credentials such as passwords and other API keys.</summary>
    public const string Interactive = nameof(Interactive);
}

/// <summary>Builds and reads the authenticated principal. Both schemes produce the same claim shape.</summary>
internal static class AuthPrincipal
{
    /// <summary>Adds the user's current roles and the given permissions, as resolved from the identity store.</summary>
    public static void AddAccessClaims(ClaimsIdentity identity, UserAccess access, IEnumerable<string> permissions)
    {
        identity.AddClaims(access.Roles.Select(role => new Claim(AppClaimTypes.Role, role)));
        identity.AddClaims(permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));
    }

    public static ClaimsPrincipal ForApiKey(UserAccess access, ApiKeyIdentity key, string scheme)
    {
        // A key narrowed to scopes gets the overlap; it can never exceed what its owner can do now.
        var permissions = key.Scopes.Count == 0
            ? access.Permissions
            : access.Permissions.Intersect(key.Scopes, StringComparer.Ordinal).ToList();

        var identity = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.Subject, access.UserId.ToString()),
                new Claim(AppClaimTypes.Name, access.DisplayName),
                new Claim(AppClaimTypes.Email, access.Email),
                new Claim(AppClaimTypes.AuthenticationMethod, "api_key"),
                new Claim(AppClaimTypes.ApiKeyId, key.KeyId.ToString()),
            ],
            scheme, AppClaimTypes.Name, AppClaimTypes.Role);
        AddAccessClaims(identity, access, permissions);
        return new ClaimsPrincipal(identity);
    }

    public static Guid GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AppClaimTypes.Subject), out var id)
            ? id
            : throw new InvalidOperationException("The authenticated principal has no user id.");

    public static Actor ToActor(this ClaimsPrincipal user) => new(
        user.GetUserId(),
        user.FindAll(AppClaimTypes.Role).Select(c => c.Value).ToList(),
        user.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToHashSet(StringComparer.Ordinal));

    public static CurrentUserDto ToCurrentUser(this ClaimsPrincipal user) => new(
        user.GetUserId(),
        user.FindFirstValue(AppClaimTypes.Email) ?? "",
        user.FindFirstValue(AppClaimTypes.Name) ?? "",
        user.FindAll(AppClaimTypes.Role).Select(c => c.Value).ToList(),
        user.FindAll(AppClaimTypes.Permission).Select(c => c.Value).Order(StringComparer.Ordinal).ToList());
}
