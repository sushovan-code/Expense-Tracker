# 0001. SQLite now, MongoDB Atlas later

- Status: Accepted
- Date: 2026-11-08

## Context
The API stores data in SQL Server through EF Core and runs locally against SQL Server LocalDB. LocalDB is Windows-only, so the integration tests can't run in Linux CI, and every contributor needs a SQL Server install. The owner plans to move the data to MongoDB Atlas later.

## Decision
1. Replace the SQL Server provider with **SQLite** (`Microsoft.EntityFrameworkCore.Sqlite`): one database file per environment, and a fresh file per integration-test class.
2. Keep every database detail behind the Application ports (`I*Repository`, `I*Queries`, `IUnitOfWork`). Application code never sees EF Core or SQLite.
3. Work around SQLite's gaps in Infrastructure:
   - `DateTimeOffset` is stored as UTC ticks so it can be compared and sorted in SQL.
   - There are no schemas.
   - Text columns use a `NOCASE` collation, so search stays case-insensitive.
4. New tables use app-generated `Guid` IDs.

## Consequences
- The app runs with no database prerequisite on Windows, macOS and Linux, and CI runs every test.
- The existing SQL Server migrations are replaced by a single SQLite baseline. The data is synthetic, so nothing is migrated.
- SQLite allows one writer at a time, which is enough for a family app. In the cloud the file needs a persistent volume until the MongoDB move.
- **For MongoDB:** the MongoDB EF Core provider has no migrations, no joins across collections and no ASP.NET Identity EF stores. The move therefore needs:
  - a MongoDB adapter for the identity store,
  - query adapters that read denormalised documents,
  - per-family filtering kept in the adapters.

  Because the ports are already in place, use cases and endpoints won't change.
