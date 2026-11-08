using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Read side of user administration: filtered, sorted, paged user lists.</summary>
public interface IUserQueries
{
    Task<PagedResult<UserSummaryDto>> ListAsync(UserListRequest request, SortSpec sort, CancellationToken cancellationToken);
}
