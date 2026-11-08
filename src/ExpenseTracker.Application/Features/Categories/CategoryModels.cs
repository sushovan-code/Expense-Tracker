using ExpenseTracker.Application.Common;

namespace ExpenseTracker.Application.Features.Categories;

/// <summary>Input for creating or updating a category.</summary>
public sealed record CategoryCommand(string Name, string Color, decimal? MonthlyBudget);

public sealed record CategoryListRequest(string? Search, PageRequest Page, string? SortBy, string? SortDir);

public sealed record CategoryDto(int Id, string Name, string Color, decimal? MonthlyBudget, int ExpenseCount);

/// <summary>Minimal shape for dropdowns and filters.</summary>
public sealed record CategoryLookupDto(int Id, string Name, string Color);

public static class CategorySorting
{
    public const string Default = "name";
    public static readonly IReadOnlyCollection<string> Fields = ["name", "expenses", "budget"];
}
