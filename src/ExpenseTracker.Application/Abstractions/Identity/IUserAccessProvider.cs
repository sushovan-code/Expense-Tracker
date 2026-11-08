using ExpenseTracker.Application.Features.Identity;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>
/// Resolves (and caches) what a user may currently do. Called on every authenticated request, so
/// implementations cache, and every change to a user's roles, claims or status must invalidate.
/// </summary>
public interface IUserAccessProvider
{
    /// <summary><c>null</c> when the user no longer exists.</summary>
    Task<UserAccess?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task InvalidateAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Used when a role's permissions change, which affects every member of the role.</summary>
    Task InvalidateAllAsync(CancellationToken cancellationToken);
}
