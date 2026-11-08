namespace ExpenseTracker.Application.Common;

/// <summary>A validated, allow-listed sort.</summary>
public readonly record struct SortSpec(string Field, bool Descending)
{
    public string Direction => Descending ? "desc" : "asc";
}
