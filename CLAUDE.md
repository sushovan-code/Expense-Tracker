# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Expense Tracker

Family expense tracking with categories, monthly budgets and live updates. Portfolio project; synthetic data only.

## Stack
.NET 10 · React + TypeScript (Vite) · SQLite via EF Core (MongoDB Atlas planned, ADR 0001) · ASP.NET Core Identity with JWT bearer and API keys · SignalR · Aspire. See @docs/architecture.md for layers, ports and the security model.

## Commands
Run from the repo root unless noted. The web app lives in `src/web/expense-tracker-web`.

- Run everything locally: `dotnet run --project src/ExpenseTracker.AppHost` (or `aspire run`). App http://localhost:5010, API http://localhost:7010 (Scalar at `/scalar`).
- Reset the local database: stop the app and delete `src/ExpenseTracker.Api/App_Data/` (migrations and the seed run again on start).
- Backend tests: `dotnet test --solution ExpenseTracker.slnx`. Tests use xUnit v3 on Microsoft Testing Platform (`global.json`), so filters are MTP options:
  - one project: `dotnet test --project tests/ExpenseTracker.Application.Tests`
  - one test: `dotnet test --project tests/ExpenseTracker.Application.Tests --filter-method "*RefreshAsync_RotatedToken*"` (also `--filter-class`)
- Frontend: `npm test`, `npm run lint` (oxlint), `npm run check` (tsc + lint + tests), one file: `npx vitest run src/features/expenses/model/validation.test.ts`. Run these inside the web folder, or with `--prefix src/web/expense-tracker-web`.
- Format check, as CI runs it: `dotnet format ExpenseTracker.slnx --verify-no-changes --severity error`
- Migrations (`dotnet tool restore` once): `dotnet ef migrations add <Name> --project src/ExpenseTracker.Infrastructure --startup-project src/ExpenseTracker.Api --output-dir Persistence/Migrations`
- Render diagrams: `pwsh -File scripts/render-diagrams.ps1` (Mermaid CLI via npx, plus Python `diagrams` for `deployment-azure.py`). CI fails if a `.mmd`/`.py` source changes without its rendered image.

## Standards (read the relevant file before working in that area)
- Coding standards, best practices and design principles (always applies): @docs/standards/coding-standards.md
- Development workflow and definition of done: @docs/standards/development-workflow.md
- Naming conventions: @docs/standards/naming-conventions.md
- Architecture and loose coupling: @docs/standards/architecture.md
- Testing: @docs/standards/testing.md
- Security: @docs/standards/security.md
- UI, design and theming: @docs/standards/ui-and-theming.md
- Caching: @docs/standards/caching.md

## Things that span several files
- **Routes:** every REST endpoint is under `ApiRoutes.Prefix` (`/api/v1`, `Api/Endpoints/ApiRoutes.cs`). The refresh cookie's path is `ApiRoutes.Auth`. The web app's base URL is `VITE_API_BASE_URL` in `configs/.env`. The SignalR hub (`/hubs/expenses`) and health checks are unversioned. Changing the prefix means touching all of these plus the integration tests (ADR 0006).
- **IDs:** every entity gets an app-generated GUID v7 from `Domain/Common/Entity`. The seeded categories have fixed GUIDs in `CategoryConfiguration` (tests use `ApiFactory.SeededFoodCategoryId`). The frontend treats IDs as strings.
- **SQLite quirks, handled in Infrastructure:**
  - `DateTimeOffset` is stored as UTC ticks (`UtcTicksConverter`, set as a convention in `AppDbContext`), so it sorts and compares in SQL.
  - Text search must use `EF.Functions.Like(column, LikePatterns.Contains(term), LikePatterns.EscapeCharacter)`. `string.Contains` becomes `instr()`, which is case-sensitive.
  - Unique or searched text columns use the `NOCASE` collation (`SqliteCollations`).
  - A relative `Data Source` resolves against the API's content root (`SqliteConnectionStrings`).
- **Identity:** the use cases (`AuthService`, `UserAdminService`, `RoleAdminService`, `ApiKeyService`) live in `Application/Features/Identity` and talk only to the ports in `Application/Abstractions/Identity`. `Infrastructure/Identity` holds the adapters over `UserManager` and EF Core (`IdentityUserStore`, `RefreshTokenStore`, `ApiKeyStore`, …). Application can't reference `Microsoft.Extensions.Options`, so settings reach it as plain records (`SessionPolicy`) registered by Infrastructure. Any change to a user's roles, claims or status must call `IUserAccessProvider.InvalidateAsync`.
- **Layer rules are tests and lint, not conventions:** `ExpenseTracker.ArchitectureTests` (dependency rule, internal use cases and adapters) and `.oxlintrc.json` (feature boundaries in the web app).
- **Integration tests** (`ExpenseTracker.Api.IntegrationTests`) host the real API against a throwaway SQLite file per test class (`ApiFactory`), with seeded users in the `@tests.local` domain.

## Always
- When you add, remove or rewire a service, data store, cache, queue, external system or main flow, update `docs/diagrams/` and README.md's Architecture section (`/update-diagrams`).
- When you add a secret, prerequisite, port or test user, update README.md's "Run it locally" section.
- Don't change the existing look and feel (layout, flows, themes, colours). New UI reuses `shared/ui` components and `app/styles/tokens.css` tokens.
- Don't change the existing RBAC or JWT bearer authentication flow. Family access is a separate layer on top.
- Windows Git Bash rewrites arguments that start with `/` (for example `/api/v1` becomes a `C:/Program Files/Git/...` path). Set `MSYS_NO_PATHCONV=1` before such commands.

## Product spec and plans
@docs/spec.md

Work in progress is tracked in `docs/adoption-plan.md` (standards gaps) and `docs/roadmap.md` (features). Decisions are in `docs/adr/`.
