using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Expenses;

public static class ExpenseErrors
{
    public static Error NotFound(int id) => Error.NotFound("Expense", id);

    public static readonly Error UnknownCategory =
        Error.Validation("categoryId", "The selected category does not exist.");

    public static readonly Error InvalidDateRange =
        Error.Validation("from", "'from' must be on or before 'to'.");

    public static Error ExportTooLarge(int maxRows) =>
        Error.Validation("from", $"Exports are limited to {maxRows:N0} rows. Narrow the date range or filters.");
}
