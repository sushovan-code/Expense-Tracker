using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Application.Abstractions.Queries;

public interface IBudgetQueries
{
    /// <summary>Every category that has a budget, with its spending in the range.</summary>
    Task<IReadOnlyList<CategorySpending>> GetBudgetedSpendingAsync(DateOnly from, DateOnly to, CancellationToken ct);

    Task<CategorySpending?> GetCategorySpendingAsync(int categoryId, DateOnly from, DateOnly to, CancellationToken ct);
}
