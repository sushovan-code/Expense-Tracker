using ExpenseTracker.Application.Features.Identity;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Issues signed access tokens. Implemented by the API, which also validates them.</summary>
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AccessTokenSubject subject);
}

/// <summary>
/// Resolves (and caches) what a user may currently do. Called on every authenticated request, so
/// implementations cache, and every change to a user's roles, claims or status must invalidate.
/// </summary>
public interface IUserAccessProvider
{
    /// <summary><c>null</c> when the user no longer exists.</summary>
    Task<UserAccess?> GetAsync(Guid userId, CancellationToken ct);

    Task InvalidateAsync(Guid userId, CancellationToken ct);

    /// <summary>Used when a role's permissions change, which affects every member of the role.</summary>
    Task InvalidateAllAsync(CancellationToken ct);
}
