using ExpenseTracker.Application.Common;

namespace ExpenseTracker.Application.Features.Expenses;

/// <summary>Input for creating or updating an expense.</summary>
public sealed record ExpenseCommand(string Description, decimal Amount, DateOnly Date, Guid CategoryId, string? Notes, bool IsPrivate = false);

/// <param name="MemberId">Only expenses created by this family member; null means everyone.</param>
public sealed record ExpenseFilter(DateOnly? From, DateOnly? To, Guid? CategoryId, string? Search, Guid? MemberId = null)
{
    public static readonly ExpenseFilter None = new(null, null, null, null);

    public bool IsFiltered => From is not null || To is not null || CategoryId is not null || MemberId is not null
        || !string.IsNullOrWhiteSpace(Search);
}

public sealed record ExpenseListRequest(ExpenseFilter Filter, PageRequest Page, string? SortBy, string? SortDir);

public sealed record ExpenseDto(
    Guid Id,
    string Description,
    decimal Amount,
    DateOnly Date,
    Guid CategoryId,
    string CategoryName,
    string CategoryColor,
    string? Notes,
    DateTimeOffset CreatedAt,
    bool IsPrivate,
    Guid CreatedByUserId);

/// <summary>A page of expenses plus the total amount across all pages of the filtered set.</summary>
public sealed record ExpensePage(IReadOnlyList<ExpenseDto> Items, int Page, int PageSize, int TotalCount, decimal TotalAmount)
    : PagedResult<ExpenseDto>(Items, Page, PageSize, TotalCount);

public sealed record BulkDeleteResult(int Deleted, IReadOnlyList<Guid> DeletedIds);

public sealed record CategoryTotal(Guid CategoryId, string CategoryName, string CategoryColor, decimal Total, int Count);

public sealed record MonthTotal(int Year, int Month, decimal Total);

public sealed record ExpenseSummary(
    decimal Total,
    int Count,
    decimal Average,
    IReadOnlyList<CategoryTotal> ByCategory,
    IReadOnlyList<MonthTotal> ByMonth);

/// <summary>Limits on exporting expenses.</summary>
public static class ExpenseExport
{
    /// <summary>Most rows one export may contain; narrower filters are required beyond this.</summary>
    public const int MaxRows = 10_000;
}

public static class ExpenseSorting
{
    public const string Default = "date";
    public static readonly IReadOnlyCollection<string> Fields = ["date", "amount", "description", "category", "createdat"];
}
