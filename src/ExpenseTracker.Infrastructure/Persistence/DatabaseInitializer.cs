using ExpenseTracker.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>Applies pending EF Core migrations, logging what it did.</summary>
public sealed partial class DatabaseInitializer(AppDbContext dbContext, ILogger<DatabaseInitializer> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            LogDatabaseUpToDate(logger);
            return;
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
        LogMigrationsApplied(logger, pending.Count, string.Join(", ", pending));
    }

    /// <summary>Drops the database (used by integration tests).</summary>
    public Task DeleteAsync(CancellationToken cancellationToken = default) => dbContext.Database.EnsureDeletedAsync(cancellationToken);

    [LoggerMessage(EventId = LogEvents.MigrationsApplied, Level = LogLevel.Information,
        Message = "Applied {MigrationCount} database migration(s): {Migrations}")]
    private static partial void LogMigrationsApplied(ILogger logger, int migrationCount, string migrations);

    [LoggerMessage(EventId = LogEvents.DatabaseUpToDate, Level = LogLevel.Information, Message = "Database schema is up to date")]
    private static partial void LogDatabaseUpToDate(ILogger logger);
}
