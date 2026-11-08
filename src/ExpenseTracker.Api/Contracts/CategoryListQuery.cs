using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;

namespace ExpenseTracker.Api.Contracts;

public sealed record CategoryListQuery(
    [property: Description("Case-insensitive match on the name.")] string? Search,
    [property: Range(1, int.MaxValue), Description("1-based page number (default 1).")] int? Page,
    [property: Range(1, Paging.MaxPageSize), Description("Items per page, 1–100 (default 20).")] int? PageSize,
    [property: Description("name (default), expenses or budget.")] string? SortBy,
    [property: Description("asc (default) or desc.")] string? SortDir)
{
    public CategoryListRequest ToRequest() => new(Search, new PageRequest(Page, PageSize), SortBy, SortDir);
}
