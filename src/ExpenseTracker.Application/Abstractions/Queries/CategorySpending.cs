namespace ExpenseTracker.Application.Abstractions.Queries;

/// <summary>Spending of one category within a date range.</summary>
public sealed record CategorySpending(int CategoryId, string Name, string Color, decimal? Budget, decimal Spent);
