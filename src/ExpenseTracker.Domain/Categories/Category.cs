using System.Text.RegularExpressions;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Domain.Categories;

/// <summary>A named, colored group of expenses with an optional monthly spending limit.</summary>
public sealed partial class Category : Entity
{
    public const int NameMaxLength = 50;
    public const decimal MaxBudget = 999_999_999m;

    private readonly List<Expense> _expenses = [];

    private Category() { } // EF Core

    public string Name { get; private set; } = "";

    /// <summary>Hex color, lower case (e.g. <c>#4f46e5</c>).</summary>
    public string Color { get; private set; } = "";

    /// <summary>Optional spending limit per calendar month.</summary>
    public decimal? MonthlyBudget { get; private set; }

    public IReadOnlyCollection<Expense> Expenses => _expenses;

    public static Result<Category> Create(string name, string color, decimal? monthlyBudget)
    {
        var category = new Category();
        var applied = category.Apply(name, color, monthlyBudget);
        return applied.IsSuccess ? category : applied.Error;
    }

    public Result Update(string name, string color, decimal? monthlyBudget) => Apply(name, color, monthlyBudget);

    /// <summary>Names are compared after trimming; uniqueness itself is checked by the application layer.</summary>
    public static string NormalizeName(string? name) => name?.Trim() ?? "";

    private Result Apply(string name, string color, decimal? monthlyBudget)
    {
        var normalizedName = NormalizeName(name);
        if (normalizedName.Length == 0)
            return Error.Validation("name", "Name is required.");
        if (normalizedName.Length > NameMaxLength)
            return Error.Validation("name", $"Name must be at most {NameMaxLength} characters.");
        if (color is null || !HexColor().IsMatch(color))
            return Error.Validation("color", "Color must be a hex value like #1a2b3c.");
        if (monthlyBudget is < 0 or > MaxBudget)
            return Error.Validation("monthlyBudget", $"Budget must be between 0 and {MaxBudget:N0}.");

        Name = normalizedName;
        Color = color.ToLowerInvariant();
        MonthlyBudget = monthlyBudget;
        return Result.Success();
    }

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();
}
