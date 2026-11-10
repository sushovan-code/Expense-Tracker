namespace ExpenseTracker.Domain.Expenses;

/// <summary>The editable details of an expense, validated as a whole.</summary>
public readonly record struct ExpenseDetails(string Description, decimal Amount, DateOnly Date, Guid CategoryId, string? Notes, bool IsPrivate = false);
