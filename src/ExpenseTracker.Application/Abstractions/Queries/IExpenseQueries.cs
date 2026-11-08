using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Application.Abstractions.Queries;

/// <summary>
/// Read side for expenses: projections straight to DTOs (CQRS-style), so queries stay fast and
/// never load aggregates they don't modify.
/// </summary>
public interface IExpenseQueries
{
    Task<ExpensePage> ListAsync(ExpenseFilter filter, PageRequest page, SortSpec sort, CancellationToken ct);
    Task<IReadOnlyList<ExpenseDto>> ListAllAsync(ExpenseFilter filter, SortSpec sort, int maxRows, CancellationToken ct);
    Task<ExpenseDto?> GetAsync(int id, CancellationToken ct);
    Task<ExpenseSummary> SummarizeAsync(DateOnly? from, DateOnly? to, CancellationToken ct);
}
