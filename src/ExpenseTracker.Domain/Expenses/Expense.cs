using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Expenses;

/// <summary>The editable details of an expense, validated as a whole.</summary>
public readonly record struct ExpenseDetails(string Description, decimal Amount, DateOnly Date, int CategoryId, string? Notes);

/// <summary>Money spent on something, on a day, in a category.</summary>
public sealed class Expense : Entity
{
    public const int DescriptionMaxLength = 200;
    public const int NotesMaxLength = 1000;
    public const decimal MinAmount = 0.01m;
    public const int MaxWholeAmount = 999_999_999;
    public const decimal MaxAmount = MaxWholeAmount;

    private Expense() { } // EF Core

    public string Description { get; private set; } = "";
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public int CategoryId { get; private set; }
    public Category? Category { get; private set; }

    public static Result<Expense> Create(ExpenseDetails details, DateTime createdAtUtc)
    {
        var expense = new Expense { CreatedAt = createdAtUtc };
        var applied = expense.Apply(details);
        return applied.IsSuccess ? expense : applied.Error;
    }

    public Result Update(ExpenseDetails details) => Apply(details);

    private Result Apply(ExpenseDetails details)
    {
        var description = details.Description?.Trim() ?? "";
        if (description.Length == 0)
            return Error.Validation("description", "Description is required.");
        if (description.Length > DescriptionMaxLength)
            return Error.Validation("description", $"Description must be at most {DescriptionMaxLength} characters.");
        if (details.Amount is < MinAmount or > MaxAmount)
            return Error.Validation("amount", $"Amount must be between {MinAmount} and {MaxAmount:N0}.");
        if (decimal.Round(details.Amount, 2) != details.Amount)
            return Error.Validation("amount", "Amount can have at most two decimal places.");
        if (details.CategoryId <= 0)
            return Error.Validation("categoryId", "Pick a category.");

        var notes = string.IsNullOrWhiteSpace(details.Notes) ? null : details.Notes.Trim();
        if (notes?.Length > NotesMaxLength)
            return Error.Validation("notes", $"Notes must be at most {NotesMaxLength} characters.");

        Description = description;
        Amount = details.Amount;
        Date = details.Date;
        CategoryId = details.CategoryId;
        Notes = notes;
        return Result.Success();
    }
}
