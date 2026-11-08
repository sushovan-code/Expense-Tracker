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
   - Searched and unique text columns use a `NOCASE` collation, so duplicate-name checks ignore case.
   - Text search uses an escaped `LIKE` pattern (`LikePatterns`), because the `instr()` that EF Core generates for `Contains` ignores collations and is case-sensitive.
   - SQLite stores `decimal` as text; EF Core translates comparisons, ordering and sums for it, so amounts need no change.
4. Every entity uses an app-generated **GUID v7** ID (`Entity.Id`), including `Expense` and `Category`, which used database-generated integers. IDs exist before saving, sort by creation time and fit any store, MongoDB included. The default categories keep fixed GUIDs in the seed data.

## Consequences
- The app runs with no database prerequisite on Windows, macOS and Linux, and CI runs every test.
- The existing SQL Server migrations are replaced by a single SQLite baseline. The data is synthetic, so nothing is migrated.
- SQLite allows one writer at a time, which is enough for a family app. In the cloud the file needs a persistent volume until the MongoDB move.
- **For MongoDB:** the MongoDB EF Core provider has no migrations, no joins across collections and no ASP.NET Identity EF stores. The move therefore needs:
  - a MongoDB adapter for the identity store,
  - query adapters that read denormalised documents,
  - per-family filtering kept in the adapters.

  Because the ports are already in place, use cases and endpoints won't change.
