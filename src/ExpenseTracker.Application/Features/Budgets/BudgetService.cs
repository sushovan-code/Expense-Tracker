using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Domain.Budgets;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Budgets;

public sealed record BudgetStatusDto(
    int CategoryId,
    string CategoryName,
    string CategoryColor,
    decimal Budget,
    decimal Spent,
    decimal Remaining,
    decimal PercentUsed,
    BudgetState State);

public sealed record BudgetOverviewDto(int Year, int Month, decimal TotalBudget, decimal TotalSpent, IReadOnlyList<BudgetStatusDto> Categories);

public interface IBudgetService
{
    /// <summary>Budget status for a month (defaults to the current month).</summary>
    Task<BudgetOverviewDto> GetOverviewAsync(int? year, int? month, CancellationToken cancellationToken);
}

/// <summary>Used by other use cases to detect the moment a category goes over its budget.</summary>
internal interface IBudgetMonitor
{
    Task<BudgetAlert?> DetectOverrunAsync(int categoryId, DateOnly date, decimal addedAmount, CancellationToken cancellationToken);
}

internal sealed partial class BudgetService(IBudgetQueries queries, TimeProvider clock, ILogger<BudgetService> logger)
    : IBudgetService, IBudgetMonitor
{
    public async Task<BudgetOverviewDto> GetOverviewAsync(int? year, int? month, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var (from, to) = BudgetPolicy.Month(year ?? today.Year, month ?? today.Month);

        var statuses = (await queries.GetBudgetedSpendingAsync(from, to, cancellationToken))
            .Where(s => s.Budget is not null)
            .Select(s => ToStatus(s, s.Budget!.Value))
            .ToList();

        LogBudgetStatus(logger, from.Year, from.Month, statuses.Count, statuses.Count(s => s.State == BudgetState.Exceeded));
        return new BudgetOverviewDto(from.Year, from.Month, statuses.Sum(s => s.Budget), statuses.Sum(s => s.Spent), statuses);
    }

    public async Task<BudgetAlert?> DetectOverrunAsync(int categoryId, DateOnly date, decimal addedAmount, CancellationToken cancellationToken)
    {
        if (addedAmount <= 0) return null;

        var (from, to) = BudgetPolicy.MonthOf(date);
        var spending = await queries.GetCategorySpendingAsync(categoryId, from, to, cancellationToken);
        if (spending?.Budget is not { } budget) return null;
        if (!BudgetPolicy.CrossedLimit(budget, spending.Spent - addedAmount, spending.Spent)) return null;

        LogBudgetExceeded(logger, categoryId, spending.Name, spending.Spent, budget, date.Year, date.Month);
        return new BudgetAlert(categoryId, spending.Name, budget, spending.Spent, date.Year, date.Month);
    }

    private static BudgetStatusDto ToStatus(CategorySpending s, decimal budget) => new(
        s.CategoryId, s.Name, s.Color, budget, s.Spent, budget - s.Spent,
        BudgetPolicy.PercentUsed(budget, s.Spent),
        BudgetPolicy.StateOf(budget, s.Spent));

    [LoggerMessage(EventId = LogEvents.BudgetStatusComputed, Level = LogLevel.Debug,
        Message = "Computed budget status for {Year}-{Month}: {BudgetedCategories} budgeted categories, {ExceededCategories} over budget")]
    private static partial void LogBudgetStatus(ILogger logger, int year, int month, int budgetedCategories, int exceededCategories);

    [LoggerMessage(EventId = LogEvents.BudgetExceeded, Level = LogLevel.Warning,
        Message = "Category {CategoryId} '{CategoryName}' exceeded its monthly budget for {Year}-{Month}: spent {Spent} of {Budget}")]
    private static partial void LogBudgetExceeded(
        ILogger logger, int categoryId, string categoryName, decimal spent, decimal budget, int year, int month);
}
