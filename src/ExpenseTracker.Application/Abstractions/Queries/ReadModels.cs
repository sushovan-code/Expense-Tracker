using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Application.Abstractions.Queries;

/// <summary>
/// Read side for expenses: projections straight to DTOs (CQRS-style), so queries stay fast and
/// never load aggregates they don't modify.
/// </summary>
public interface IExpenseQueries
{
    Task<ExpensePage> ListAsync(ExpenseFilter filter, PageRequest page, SortSpec sort, CancellationToken ct);
    Task<IReadOnlyList<ExpenseDto>> ListAllAsync(ExpenseFilter filter, SortSpec sort, int maxRows, CancellationToken ct);
    Task<ExpenseDto?> GetAsync(int id, CancellationToken ct);
    Task<ExpenseSummary> SummarizeAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
}

public interface ICategoryQueries
{
    Task<PagedResult<CategoryDto>> ListAsync(string? search, PageRequest page, SortSpec sort, CancellationToken ct);
    Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken ct);
}

/// <summary>Spending of one category within a date range.</summary>
public sealed record CategorySpending(int CategoryId, string Name, string Color, decimal? Budget, decimal Spent);

public interface IBudgetQueries
{
    /// <summary>Every category that has a budget, with its spending in the range.</summary>
    Task<IReadOnlyList<CategorySpending>> GetBudgetedSpendingAsync(DateOnly from, DateOnly to, CancellationToken ct);

    Task<CategorySpending?> GetCategorySpendingAsync(int categoryId, DateOnly from, DateOnly to, CancellationToken ct);
}
