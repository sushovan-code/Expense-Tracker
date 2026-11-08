namespace ExpenseTracker.Api.Contracts;

/// <summary>
/// Input limits used only by the HTTP contracts. Limits that are business rules come from the
/// domain (<c>Expense</c>, <c>Category</c>) or <c>CredentialLimits</c> instead, so they are defined once.
/// </summary>
internal static class ContractLimits
{
    /// <summary>Same length as the identity store's email and user-name columns.</summary>
    public const int EmailMaxLength = 256;
    public const int DisplayNameMaxLength = 100;

    public const int ClaimTypeMaxLength = 256;
    public const int ClaimValueMaxLength = 1024;

    /// <summary>The smallest amount (one cent or paisa). The domain enforces it exactly as <c>Expense.MinAmount</c>.</summary>
    public const double SmallestAmount = 0.01;

    public const int MinBudgetYear = 2000;
    public const int MaxBudgetYear = 2100;
    public const int FirstMonth = 1;
    public const int LastMonth = 12;
}
