using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Application.Abstractions.Queries;

/// <summary>Spending of one category within a date range.</summary>
public sealed record CategorySpending(int CategoryId, string Name, string Color, decimal? Budget, decimal Spent);
