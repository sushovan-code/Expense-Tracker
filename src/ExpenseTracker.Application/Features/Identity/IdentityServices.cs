using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Features.Identity;

// Identity use cases. They're thin orchestration over the identity store (ASP.NET Core Identity), so
// they're implemented next to it in Infrastructure; the rules they enforce live in AccessPolicy.

/// <summary>Sign-in, session renewal and self-service account operations.</summary>
public interface IAuthService
{
    Task<Result<AuthSession>> LoginAsync(LoginCommand command, string? ipAddress, CancellationToken ct);

    /// <summary>Rotates a refresh token. Presenting an already-rotated token revokes its whole session (reuse detection).</summary>
    Task<Result<AuthSession>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct);

    /// <summary>Revokes the session the refresh token belongs to. Unknown tokens are ignored.</summary>
    Task LogoutAsync(string? refreshToken, CancellationToken ct);

    /// <summary>Changes the password and signs out every other session (the one holding <paramref name="currentRefreshToken"/> stays).</summary>
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordCommand command, string? currentRefreshToken, CancellationToken ct);
}

/// <summary>User, role assignment and user claim administration (Admin or above).</summary>
public interface IUserAdminService
{
    Task<Result<PagedResult<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken ct);
    Task<Result<UserDetailDto>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<UserDetailDto>> CreateAsync(Actor actor, CreateUserCommand command, CancellationToken ct);
    Task<Result<UserDetailDto>> UpdateAsync(Actor actor, Guid id, UpdateUserCommand command, CancellationToken ct);
    Task<Result<UserDetailDto>> SetRolesAsync(Actor actor, Guid id, IReadOnlyList<string> roles, CancellationToken ct);
    Task<Result<UserDetailDto>> AddClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken ct);
    Task<Result<UserDetailDto>> RemoveClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken ct);
    Task<Result> ResetPasswordAsync(Actor actor, Guid id, string newPassword, CancellationToken ct);
    Task<Result> UnlockAsync(Actor actor, Guid id, CancellationToken ct);
    /// <summary>Signs the user out everywhere: revokes refresh tokens and invalidates issued access tokens.</summary>
    Task<Result> RevokeSessionsAsync(Actor actor, Guid id, CancellationToken ct);
    Task<Result> DeleteAsync(Actor actor, Guid id, CancellationToken ct);
}

/// <summary>Role → permission administration.</summary>
public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(Actor actor, CancellationToken ct);
    Task<Result<RoleDto>> SetPermissionsAsync(Actor actor, string role, IReadOnlyList<string> permissions, CancellationToken ct);
}

/// <summary>Personal API keys (sent as <c>x-api-key</c>).</summary>
public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId, CancellationToken ct);
    Task<Result<CreatedApiKeyDto>> CreateAsync(Actor actor, CreateApiKeyCommand command, CancellationToken ct);
    Task<Result> RevokeAsync(Guid userId, Guid keyId, CancellationToken ct);

    /// <summary>Resolves a presented key; <c>null</c> when it is unknown, revoked or expired.</summary>
    Task<ApiKeyIdentity?> ValidateAsync(string key, CancellationToken ct);
}
