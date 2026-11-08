namespace ExpenseTracker.Application.Abstractions.Queries;

public interface IBudgetQueries
{
    /// <summary>Every category that has a budget, with its spending in the range.</summary>
    Task<IReadOnlyList<CategorySpending>> GetBudgetedSpendingAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);

    Task<CategorySpending?> GetCategorySpendingAsync(Guid categoryId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
