using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Features.Identity;

// Identity use cases. They orchestrate through the identity ports in Abstractions/Identity (implemented over
// ASP.NET Core Identity in Infrastructure); the rules they enforce live in AccessPolicy.

/// <summary>Sign-in, session renewal and self-service account operations.</summary>
public interface IAuthService
{
    Task<Result<AuthSession>> LoginAsync(LoginCommand command, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Rotates a refresh token. Presenting an already-rotated token revokes its whole session (reuse detection).</summary>
    Task<Result<AuthSession>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken);

    /// <summary>Revokes the session the refresh token belongs to. Unknown tokens are ignored.</summary>
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken);

    /// <summary>Changes the password and signs out every other session (the one holding <paramref name="currentRefreshToken"/> stays).</summary>
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordCommand command, string? currentRefreshToken, CancellationToken cancellationToken);
}

/// <summary>User, role assignment and user claim administration (Admin or above).</summary>
public interface IUserAdminService
{
    Task<Result<PagedResult<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> CreateAsync(Actor actor, CreateUserCommand command, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> UpdateAsync(Actor actor, Guid id, UpdateUserCommand command, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> SetRolesAsync(Actor actor, Guid id, IReadOnlyList<string> roles, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> AddClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken cancellationToken);
    Task<Result<UserDetailDto>> RemoveClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken cancellationToken);
    Task<Result> ResetPasswordAsync(Actor actor, Guid id, string newPassword, CancellationToken cancellationToken);
    Task<Result> UnlockAsync(Actor actor, Guid id, CancellationToken cancellationToken);
    /// <summary>Signs the user out everywhere: revokes refresh tokens and invalidates issued access tokens.</summary>
    Task<Result> RevokeSessionsAsync(Actor actor, Guid id, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Actor actor, Guid id, CancellationToken cancellationToken);
}

/// <summary>Role → permission administration.</summary>
public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(Actor actor, CancellationToken cancellationToken);
    Task<Result<RoleDto>> SetPermissionsAsync(Actor actor, string role, IReadOnlyList<string> permissions, CancellationToken cancellationToken);
}

/// <summary>Personal API keys (sent as <c>x-api-key</c>).</summary>
public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result<CreatedApiKeyDto>> CreateAsync(Actor actor, CreateApiKeyCommand command, CancellationToken cancellationToken);
    Task<Result> RevokeAsync(Guid userId, Guid keyId, CancellationToken cancellationToken);

    /// <summary>Resolves a presented key; <c>null</c> when it is unknown, revoked or expired.</summary>
    Task<ApiKeyIdentity?> ValidateAsync(string key, CancellationToken cancellationToken);
}
