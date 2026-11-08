using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Application.Abstractions.Queries;

public interface ICategoryQueries
{
    Task<PagedResult<CategoryDto>> ListAsync(string? search, PageRequest page, SortSpec sort, CancellationToken ct);
    Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken ct);
}
