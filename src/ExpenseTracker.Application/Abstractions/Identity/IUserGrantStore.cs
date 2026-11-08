using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>A user's role memberships and direct claims.</summary>
public interface IUserGrantStore
{
    Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result> AddToRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
    Task<Result> RemoveFromRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimDto>> GetClaimsAsync(Guid userId, CancellationToken cancellationToken);
    Task<Result> AddClaimAsync(Guid userId, ClaimDto claim, CancellationToken cancellationToken);
    Task<Result> RemoveClaimAsync(Guid userId, ClaimDto claim, CancellationToken cancellationToken);
}
