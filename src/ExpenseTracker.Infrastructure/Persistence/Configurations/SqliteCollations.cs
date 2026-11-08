namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

/// <summary>Column collations used with SQLite.</summary>
internal static class SqliteCollations
{
    /// <summary>
    /// Case-insensitive for ASCII. Used on searched and unique text columns so search and duplicate checks ignore
    /// case, as they did on SQL Server's default collation.
    /// </summary>
    public const string CaseInsensitive = "NOCASE";
}
