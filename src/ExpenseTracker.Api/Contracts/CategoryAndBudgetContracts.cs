using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Domain.Categories;

namespace ExpenseTracker.Api.Contracts;

public sealed record CategoryRequest(
    [property: Required, StringLength(Category.NameMaxLength, MinimumLength = 1), Description("Unique display name.")]
    string Name,
    [property: Required,
               RegularExpression(Category.HexColorPattern, ErrorMessage = "Color must be a hex value like #1a2b3c."),
               Description("Hex color, e.g. #4f46e5.")]
    string Color,
    [property: Range(0, Category.MaxWholeBudget), Description("Optional monthly spending limit.")]
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
    [property: Range(ContractLimits.MinBudgetYear, ContractLimits.MaxBudgetYear), Description("Year (defaults to the current year).")] int? Year,
    [property: Range(ContractLimits.FirstMonth, ContractLimits.LastMonth), Description("Month 1–12 (defaults to the current month).")] int? Month);
