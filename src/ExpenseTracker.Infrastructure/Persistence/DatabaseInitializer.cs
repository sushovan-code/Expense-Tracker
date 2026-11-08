using ExpenseTracker.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations, logging what it did.</summary>
public sealed partial class DatabaseInitializer(AppDbContext db, ILogger<DatabaseInitializer> logger)
{
    public async Task MigrateAsync(CancellationToken ct = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            LogDatabaseUpToDate(logger);
            return;
        }

        await db.Database.MigrateAsync(ct);
        LogMigrationsApplied(logger, pending.Count, string.Join(", ", pending));
    }

    /// <summary>Drops the database (used by integration tests).</summary>
    public Task DeleteAsync(CancellationToken ct = default) => db.Database.EnsureDeletedAsync(ct);

    [LoggerMessage(EventId = LogEvents.MigrationsApplied, Level = LogLevel.Information,
        Message = "Applied {MigrationCount} database migration(s): {Migrations}")]
    private static partial void LogMigrationsApplied(ILogger logger, int migrationCount, string migrations);

    [LoggerMessage(EventId = LogEvents.DatabaseUpToDate, Level = LogLevel.Information, Message = "Database schema is up to date")]
    private static partial void LogDatabaseUpToDate(ILogger logger);
}
