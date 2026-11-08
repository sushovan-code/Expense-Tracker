using Microsoft.Data.Sqlite;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>Makes a SQLite connection string independent of the process's working directory.</summary>
internal static class SqliteConnectionStrings
{
    private const string InMemoryDataSource = ":memory:";

    /// <summary>
    /// Resolves a relative <c>Data Source</c> (for example <c>App_Data/expense-tracker.db</c>) against
    /// <paramref name="basePath"/> and creates its folder, so the API, Aspire and EF tooling all use the same file.
    /// </summary>
    public static string ResolveDataSource(string connectionString, string basePath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource) || dataSource == InMemoryDataSource || builder.Mode == SqliteOpenMode.Memory)
        {
            return connectionString;
        }

        var fullPath = Path.GetFullPath(Path.IsPathRooted(dataSource) ? dataSource : Path.Combine(basePath, dataSource));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        builder.DataSource = fullPath;
        return builder.ToString();
    }
}
