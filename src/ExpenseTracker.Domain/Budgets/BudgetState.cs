namespace ExpenseTracker.Domain.Budgets;

public enum BudgetState
{
    /// <summary>Below the warning threshold.</summary>
    Ok,

    /// <summary>At or above the warning threshold, but within budget.</summary>
    Warning,

    /// <summary>Spent more than the budget.</summary>
    Exceeded,
}
