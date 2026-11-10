using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Families;

/// <summary>A group of people who share expenses, categories and budgets.</summary>
public sealed class Family : Entity
{
    public const int NameMaxLength = 100;

    private Family() { } // EF Core

    public string Name { get; private set; } = "";
    public DateTimeOffset CreatedAt { get; private set; }

    public static Result<Family> Create(string name, DateTimeOffset createdAt)
    {
        var family = new Family { CreatedAt = createdAt };
        var named = family.Rename(name);
        return named.IsSuccess ? family : named.Error;
    }

    public Result Rename(string name)
    {
        var trimmed = name?.Trim() ?? "";
        if (trimmed.Length == 0) return Error.Validation("name", "Name is required.");
        if (trimmed.Length > NameMaxLength) return Error.Validation("name", $"Name must be at most {NameMaxLength} characters.");
        Name = trimmed;
        return Result.Success();
    }
}
