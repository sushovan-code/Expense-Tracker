using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;

namespace ExpenseTracker.Application.Abstractions.Queries;

public interface ICategoryQueries
{
    Task<PagedResult<CategoryDto>> ListAsync(string? search, PageRequest page, SortSpec sort, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken cancellationToken);
}
