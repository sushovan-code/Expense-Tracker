using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Api.Contracts;

// HTTP request shapes. Attributes give fast, framework-level 400s and document the API;
// the domain enforces the same rules again, so other entry points stay safe too.

public sealed record ExpenseRequest(
    [property: Required, StringLength(200, MinimumLength = 1), Description("What the money was spent on.")]
    string Description,
    [property: Range(typeof(decimal), "0.01", "999999999"), Description("Amount spent, up to two decimal places.")]
    decimal Amount,
    [property: Description("Date of the expense (YYYY-MM-DD).")]
    DateOnly Date,
    [property: Range(1, int.MaxValue), Description("Id of an existing category.")]
    int CategoryId,
    [property: StringLength(1000)]
    string? Notes)
{
    public ExpenseCommand ToCommand() => new(Description, Amount, Date, CategoryId, Notes);
}

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

public sealed record ExpenseExportQuery(DateOnly? From, DateOnly? To, int? CategoryId, string? Search, string? SortBy, string? SortDir)
{
    public ExpenseFilter ToFilter() => new(From, To, CategoryId, Search);
}

public sealed record SummaryQuery(DateOnly? From, DateOnly? To);

public sealed record BulkDeleteRequest(
    [property: Required, MinLength(1), MaxLength(500), Description("Ids of the expenses to delete (max 500).")]
    int[] Ids);
