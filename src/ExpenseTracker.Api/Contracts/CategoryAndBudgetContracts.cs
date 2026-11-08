using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;

namespace ExpenseTracker.Api.Contracts;

public sealed record CategoryRequest(
    [property: Required, StringLength(50, MinimumLength = 1), Description("Unique display name.")]
    string Name,
    [property: Required,
               RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Color must be a hex value like #1a2b3c."),
               Description("Hex color, e.g. #4f46e5.")]
    string Color,
    [property: Range(typeof(decimal), "0", "999999999"), Description("Optional monthly spending limit.")]
    decimal? MonthlyBudget)
{
    public CategoryCommand ToCommand() => new(Name, Color, MonthlyBudget);
}

public sealed record CategoryListQuery(
    [property: Description("Case-insensitive match on the name.")] string? Search,
    [property: Range(1, int.MaxValue), Description("1-based page number (default 1).")] int? Page,
    [property: Range(1, Paging.MaxPageSize), Description("Items per page, 1–100 (default 20).")] int? PageSize,
    [property: Description("name (default), expenses or budget.")] string? SortBy,
    [property: Description("asc (default) or desc.")] string? SortDir)
{
    public CategoryListRequest ToRequest() => new(Search, new PageRequest(Page, PageSize), SortBy, SortDir);
}

public sealed record BudgetQuery(
    [property: Range(2000, 2100), Description("Year (defaults to the current year).")] int? Year,
    [property: Range(1, 12), Description("Month 1–12 (defaults to the current month).")] int? Month);
