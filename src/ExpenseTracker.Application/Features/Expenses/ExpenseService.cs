using System.Diagnostics;
using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Budgets;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Expenses;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Expenses;

public interface IExpenseService
{
    Task<Result<ExpensePage>> ListAsync(ExpenseListRequest request, CancellationToken ct);
    Task<Result<IReadOnlyList<ExpenseDto>>> ExportAsync(ExpenseFilter filter, string? sortBy, string? sortDir, CancellationToken ct);
    Task<Result<ExpenseSummary>> SummarizeAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<Result<ExpenseDto>> GetAsync(int id, CancellationToken ct);
    Task<Result<ExpenseDto>> CreateAsync(ExpenseCommand command, CancellationToken ct);
    Task<Result<ExpenseDto>> UpdateAsync(int id, ExpenseCommand command, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);
    Task<Result<BulkDeleteResult>> BulkDeleteAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}

internal sealed partial class ExpenseService(
    IExpenseRepository expenses,
    ICategoryRepository categories,
    IUnitOfWork unitOfWork,
    IExpenseQueries queries,
    IBudgetMonitor budgets,
    IChangeNotifier notifier,
    TimeProvider clock,
    ILogger<ExpenseService> logger) : IExpenseService
{
    internal const int MaxExportRows = 10_000;

    public async Task<Result<ExpensePage>> ListAsync(ExpenseListRequest request, CancellationToken ct)
    {
        if (!IsValidRange(request.Filter)) return ExpenseErrors.InvalidDateRange;
        var sort = Paging.ResolveSort(request.SortBy, request.SortDir, ExpenseSorting.Default, defaultDescending: true, ExpenseSorting.Fields);
        if (!sort.IsSuccess) return sort.Error;

        var started = Stopwatch.GetTimestamp();
        var page = await queries.ListAsync(request.Filter, request.Page, sort.Value, ct);

        LogExpensesListed(logger, page.Items.Count, page.TotalCount, page.Page, page.PageSize, sort.Value.Field,
            sort.Value.Direction, request.Filter.IsFiltered, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return page;
    }

    public async Task<Result<IReadOnlyList<ExpenseDto>>> ExportAsync(
        ExpenseFilter filter, string? sortBy, string? sortDir, CancellationToken ct)
    {
        if (!IsValidRange(filter)) return ExpenseErrors.InvalidDateRange;
        var sort = Paging.ResolveSort(sortBy, sortDir, ExpenseSorting.Default, defaultDescending: true, ExpenseSorting.Fields);
        if (!sort.IsSuccess) return sort.Error;

        var rows = await queries.ListAllAsync(filter, sort.Value, MaxExportRows + 1, ct);
        if (rows.Count > MaxExportRows) return ExpenseErrors.ExportTooLarge(MaxExportRows);

        LogExpensesExported(logger, rows.Count, filter.IsFiltered);
        return Result<IReadOnlyList<ExpenseDto>>.Success(rows);
    }

    public async Task<Result<ExpenseSummary>> SummarizeAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        if (from > to) return ExpenseErrors.InvalidDateRange;

        var summary = await queries.SummarizeAsync(from, to, ct);
        LogSummaryComputed(logger, from, to, summary.Count, summary.Total);
        return summary;
    }

    public async Task<Result<ExpenseDto>> GetAsync(int id, CancellationToken ct)
    {
        var expense = await queries.GetAsync(id, ct);
        if (expense is not null) return expense;

        LogExpenseNotFound(logger, id);
        return ExpenseErrors.NotFound(id);
    }

    public async Task<Result<ExpenseDto>> CreateAsync(ExpenseCommand command, CancellationToken ct)
    {
        var category = await categories.GetByIdAsync(command.CategoryId, ct);
        if (category is null) return ExpenseErrors.UnknownCategory;

        var created = Expense.Create(ToDetails(command), clock.GetUtcNow().UtcDateTime);
        if (!created.IsSuccess) return created.Error;
        var expense = created.Value;

        expenses.Add(expense);
        await unitOfWork.SaveChangesAsync(ct);

        LogExpenseCreated(logger, expense.Id, expense.Amount, category.Id, category.Name, expense.Date);
        await notifier.ExpensesChangedAsync(ChangeAction.Created, [expense.Id], ct);
        await CheckBudgetAsync(expense.CategoryId, expense.Date, expense.Amount, ct);

        return ToDto(expense, category);
    }

    public async Task<Result<ExpenseDto>> UpdateAsync(int id, ExpenseCommand command, CancellationToken ct)
    {
        var expense = await expenses.GetByIdAsync(id, ct);
        if (expense is null)
        {
            LogExpenseNotFound(logger, id);
            return ExpenseErrors.NotFound(id);
        }

        var category = await categories.GetByIdAsync(command.CategoryId, ct);
        if (category is null) return ExpenseErrors.UnknownCategory;

        var before = (expense.Amount, expense.CategoryId, expense.Date);
        var updated = expense.Update(ToDetails(command));
        if (!updated.IsSuccess) return updated.Error;
        await unitOfWork.SaveChangesAsync(ct);

        LogExpenseUpdated(logger, expense.Id, before.Amount, expense.Amount, before.CategoryId, expense.CategoryId,
            before.Date, expense.Date);
        await notifier.ExpensesChangedAsync(ChangeAction.Updated, [expense.Id], ct);

        // Only the amount newly added to this category's month can push it over budget.
        var sameMonthAndCategory = before.CategoryId == expense.CategoryId
            && before.Date.Year == expense.Date.Year && before.Date.Month == expense.Date.Month;
        await CheckBudgetAsync(expense.CategoryId, expense.Date,
            sameMonthAndCategory ? expense.Amount - before.Amount : expense.Amount, ct);

        return ToDto(expense, category);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct)
    {
        if (!await expenses.DeleteAsync(id, ct))
        {
            LogExpenseNotFound(logger, id);
            return ExpenseErrors.NotFound(id);
        }

        LogExpenseDeleted(logger, id);
        await notifier.ExpensesChangedAsync(ChangeAction.Deleted, [id], ct);
        return Result.Success();
    }

    public async Task<Result<BulkDeleteResult>> BulkDeleteAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var requested = ids.Distinct().ToArray();
        var deleted = await expenses.DeleteManyAsync(requested, ct);

        LogExpensesBulkDeleted(logger, deleted.Count, requested.Length, requested.Length - deleted.Count);
        if (deleted.Count > 0) await notifier.ExpensesChangedAsync(ChangeAction.Deleted, deleted, ct);

        return new BulkDeleteResult(deleted.Count, deleted);
    }

    private async Task CheckBudgetAsync(int categoryId, DateOnly date, decimal addedAmount, CancellationToken ct)
    {
        if (await budgets.DetectOverrunAsync(categoryId, date, addedAmount, ct) is { } alert)
        {
            await notifier.BudgetExceededAsync(alert, ct);
        }
    }

    private static bool IsValidRange(ExpenseFilter filter) => !(filter.From > filter.To);

    private static ExpenseDetails ToDetails(ExpenseCommand c) => new(c.Description, c.Amount, c.Date, c.CategoryId, c.Notes);

    private static ExpenseDto ToDto(Expense e, Category c) =>
        new(e.Id, e.Description, e.Amount, e.Date, e.CategoryId, c.Name, c.Color, e.Notes, e.CreatedAt);

    // Logs carry ids, amounts and dates — never free-text descriptions or notes, which may contain personal data.

    [LoggerMessage(EventId = LogEvents.ExpenseCreated, Level = LogLevel.Information,
        Message = "Created expense {ExpenseId}: {Amount} in category {CategoryId} '{CategoryName}' on {ExpenseDate}")]
    private static partial void LogExpenseCreated(
        ILogger logger, int expenseId, decimal amount, int categoryId, string categoryName, DateOnly expenseDate);

    [LoggerMessage(EventId = LogEvents.ExpenseUpdated, Level = LogLevel.Information,
        Message = "Updated expense {ExpenseId}: amount {PreviousAmount} → {Amount}, category {PreviousCategoryId} → {CategoryId}, date {PreviousDate} → {ExpenseDate}")]
    private static partial void LogExpenseUpdated(
        ILogger logger, int expenseId, decimal previousAmount, decimal amount, int previousCategoryId, int categoryId,
        DateOnly previousDate, DateOnly expenseDate);

    [LoggerMessage(EventId = LogEvents.ExpenseDeleted, Level = LogLevel.Information, Message = "Deleted expense {ExpenseId}")]
    private static partial void LogExpenseDeleted(ILogger logger, int expenseId);

    [LoggerMessage(EventId = LogEvents.ExpensesBulkDeleted, Level = LogLevel.Information,
        Message = "Bulk-deleted {DeletedCount} of {RequestedCount} expenses ({MissingCount} already gone)")]
    private static partial void LogExpensesBulkDeleted(ILogger logger, int deletedCount, int requestedCount, int missingCount);

    [LoggerMessage(EventId = LogEvents.ExpenseNotFound, Level = LogLevel.Warning, Message = "Expense {ExpenseId} was not found")]
    private static partial void LogExpenseNotFound(ILogger logger, int expenseId);

    [LoggerMessage(EventId = LogEvents.ExpensesListed, Level = LogLevel.Debug,
        Message = "Listed {ItemCount} of {TotalCount} expenses (page {Page}, size {PageSize}, sort {SortBy} {SortDir}, filtered {Filtered}) in {ElapsedMs:0.0} ms")]
    private static partial void LogExpensesListed(
        ILogger logger, int itemCount, int totalCount, int page, int pageSize, string sortBy, string sortDir, bool filtered, double elapsedMs);

    [LoggerMessage(EventId = LogEvents.ExpenseSummaryComputed, Level = LogLevel.Debug,
        Message = "Computed spending summary for {From} to {To}: {ExpenseCount} expenses totalling {Total}")]
    private static partial void LogSummaryComputed(ILogger logger, DateOnly? from, DateOnly? to, int expenseCount, decimal total);

    [LoggerMessage(EventId = LogEvents.ExpensesExported, Level = LogLevel.Information,
        Message = "Exported {RowCount} expenses to CSV (filtered {Filtered})")]
    private static partial void LogExpensesExported(ILogger logger, int rowCount, bool filtered);
}
