using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
