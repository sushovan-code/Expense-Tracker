namespace ExpenseTracker.Domain.Budgets;

/// <summary>Rules for interpreting spending against a monthly budget.</summary>
public static class BudgetPolicy
{
    /// <summary>Percentage of the budget at which a category is flagged as close to its limit.</summary>
    public const decimal WarningThresholdPercent = 80m;

    /// <summary>Spent as a percentage of the budget, rounded to one decimal for display.</summary>
    public static decimal PercentUsed(decimal budget, decimal spent) =>
        budget == 0 ? (spent > 0 ? 100m : 0m) : Math.Round(spent / budget * 100, 1);

    /// <summary>Decided on exact amounts, never on the rounded percentage (1000.01 of 1000 is over budget).</summary>
    public static BudgetState StateOf(decimal budget, decimal spent) =>
        spent > budget ? BudgetState.Exceeded
        : spent >= budget * (WarningThresholdPercent / 100m) && spent > 0 ? BudgetState.Warning
        : BudgetState.Ok;

    /// <summary>True when spending went from within budget to over it (alert once, on the crossing).</summary>
    public static bool CrossedLimit(decimal budget, decimal spentBefore, decimal spentAfter) =>
        spentBefore <= budget && spentAfter > budget;

    /// <summary>The calendar month containing <paramref name="date"/>, as an inclusive date range.</summary>
    public static (DateOnly From, DateOnly To) MonthOf(DateOnly date) => Month(date.Year, date.Month);

    public static (DateOnly From, DateOnly To) Month(int year, int month)
    {
        var from = new DateOnly(year, month, 1);
        return (from, from.AddMonths(1).AddDays(-1));
    }
}
