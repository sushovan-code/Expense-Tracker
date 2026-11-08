using System.ComponentModel;
using ExpenseTracker.Application.Common;

namespace ExpenseTracker.Application.Features.Identity;

// ---- Authentication ----------------------------------------------------------------------------

/// <summary>The signed-in user as the client sees them: who they are and what they may do.</summary>
public sealed record CurrentUserDto(
    Guid Id, string Email, string DisplayName, IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed record LoginCommand(string Email, string Password);

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword);

/// <summary>A signed-in session: a short-lived access token plus the refresh token that renews it.</summary>
public sealed record AuthSession(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    CurrentUserDto User);

/// <summary>
/// What a user may currently do, resolved from the identity store. Authentication rebuilds every
/// principal from this (not from token contents), so role and permission changes apply immediately.
/// Immutable, so caches can hand out one shared instance.
/// </summary>
[ImmutableObject(true)]
public sealed record UserAccess(
    Guid UserId,
    string Email,
    string DisplayName,
    string SecurityStamp,
    bool IsActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions)
{
    public CurrentUserDto ToCurrentUser() => new(UserId, Email, DisplayName, Roles, Permissions);
}

/// <summary>Claims that go into an access token.</summary>
public sealed record AccessTokenSubject(Guid UserId, string Email, string DisplayName, string SecurityStamp);

public sealed record IssuedAccessToken(string Token, DateTimeOffset ExpiresAt);

// ---- User administration -----------------------------------------------------------------------

public sealed record ClaimDto(string Type, string Value);

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);

public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    bool IsLockedOut,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<ClaimDto> Claims,
    IReadOnlyList<string> EffectivePermissions);

public sealed record UserListRequest(string? Search, string? Role, PageRequest Page, string? SortBy, string? SortDir);

public sealed record CreateUserCommand(string Email, string DisplayName, string Password, IReadOnlyList<string> Roles);

public sealed record UpdateUserCommand(string Email, string DisplayName, bool IsActive);

public static class UserSorting
{
    public const string Default = "name";
    public static readonly IReadOnlyCollection<string> Fields = ["name", "email", "created", "lastlogin"];
}

// ---- Role administration -----------------------------------------------------------------------

/// <param name="CanEdit">Whether the caller may change this role's permissions.</param>
public sealed record RoleDto(
    string Name, int Rank, string Description, IReadOnlyList<string> Permissions, int UserCount, bool CanEdit);

// ---- API keys ----------------------------------------------------------------------------------

/// <param name="Scopes">Permissions the key is limited to; empty means "everything the owner can do".</param>
public sealed record ApiKeyDto(
    Guid Id,
    string Name,
    string Prefix,
    IReadOnlyList<string> Scopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);

/// <param name="Key">The full key. Shown once: only its hash is stored.</param>
public sealed record CreatedApiKeyDto(ApiKeyDto ApiKey, string Key);

public sealed record CreateApiKeyCommand(string Name, IReadOnlyList<string>? Scopes, int ExpiresInDays);

/// <summary>A valid, active API key: whose it is and what it is limited to.</summary>
public sealed record ApiKeyIdentity(Guid KeyId, Guid UserId, IReadOnlyList<string> Scopes);
