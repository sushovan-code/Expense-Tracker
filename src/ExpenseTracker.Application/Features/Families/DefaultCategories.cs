namespace ExpenseTracker.Application.Features.Families;

/// <summary>The categories every new family starts with (they used to be global seed data).</summary>
public static class DefaultCategories
{
    public static readonly IReadOnlyList<(string Name, string Color)> All =
    [
        ("Food", "#f97316"),
        ("Transport", "#3b82f6"),
        ("Housing", "#8b5cf6"),
        ("Utilities", "#06b6d4"),
        ("Entertainment", "#ec4899"),
        ("Health", "#22c55e"),
        ("Shopping", "#eab308"),
        ("Other", "#64748b"),
    ];
}
