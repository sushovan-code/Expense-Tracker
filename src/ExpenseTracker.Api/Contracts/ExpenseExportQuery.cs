using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Api.Contracts;

public sealed record ExpenseExportQuery(DateOnly? From, DateOnly? To, Guid? CategoryId, string? Search, string? SortBy, string? SortDir)
{
    public ExpenseFilter ToFilter() => new(From, To, CategoryId, Search);
}
