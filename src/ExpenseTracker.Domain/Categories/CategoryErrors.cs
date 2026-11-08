using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Categories;

public static class CategoryErrors
{
    public static Error NotFound(int id) => Error.NotFound("Category", id);

    public static readonly Error DuplicateName =
        Error.Validation("name", "A category with this name already exists.");

    /// <summary>A category can't be deleted while expenses still reference it.</summary>
    public static Error InUse(string name, int expenseCount) =>
        Error.Conflict("Category in use", $"\"{name}\" still has {expenseCount} expense(s). Move or delete them first.");
}
