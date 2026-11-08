namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Builds <c>LIKE</c> patterns for "contains" searches. SQLite's <c>LIKE</c> ignores case (ASCII), while the
/// <c>instr()</c> that EF Core uses for <see cref="string.Contains(string)"/> doesn't, so text search goes through
/// <c>EF.Functions.Like(column, LikePatterns.Contains(term), LikePatterns.EscapeCharacter)</c>.
/// </summary>
internal static class LikePatterns
{
    public const string EscapeCharacter = "\\";

    /// <summary>A pattern matching values that contain <paramref name="term"/>, with wildcards in it taken literally.</summary>
    public static string Contains(string term) => $"%{Escape(term)}%";

    private static string Escape(string term) =>
        term.Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);
}
