namespace ExpenseTracker.Application.Abstractions.Realtime;

public sealed record BudgetAlert(int CategoryId, string CategoryName, decimal Budget, decimal Spent, int Year, int Month);
