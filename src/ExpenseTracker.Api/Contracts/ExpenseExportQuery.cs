using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Api.Contracts;

public sealed record ExpenseExportQuery(DateOnly? From, DateOnly? To, int? CategoryId, string? Search, string? SortBy, string? SortDir)
{
    public ExpenseFilter ToFilter() => new(From, To, CategoryId, Search);
}
