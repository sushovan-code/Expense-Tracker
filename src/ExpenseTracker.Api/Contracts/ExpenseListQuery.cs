using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Api.Contracts;

public sealed record ExpenseListQuery(
    [property: Description("Only include expenses on or after this date.")] DateOnly? From,
    [property: Description("Only include expenses on or before this date.")] DateOnly? To,
    [property: Description("Only include expenses in this category.")] int? CategoryId,
    [property: Description("Case-insensitive match on description or notes.")] string? Search,
    [property: Range(1, int.MaxValue), Description("1-based page number (default 1).")] int? Page,
    [property: Range(1, Paging.MaxPageSize), Description("Items per page, 1–100 (default 20).")] int? PageSize,
    [property: Description("date (default), amount, description, category or createdAt.")] string? SortBy,
    [property: Description("asc or desc (default desc for date, asc otherwise).")] string? SortDir)
{
    public ExpenseListRequest ToRequest() =>
        new(new ExpenseFilter(From, To, CategoryId, Search), new PageRequest(Page, PageSize), SortBy, SortDir);
}
