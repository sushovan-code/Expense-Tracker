# Standards adoption plan

This document lists the gaps between the codebase and three standards documents: [`naming-conventions.md`](standards/naming-conventions.md), [`architecture.md`](standards/architecture.md) and [`coding-standards.md`](standards/coding-standards.md). The gaps were found in a review on 2026-11-08.

Gaps are grouped by area and ordered by impact within each group:

- **H** (high): breaks the build or CI, or is a security or architecture flaw a reviewer would flag at once.
- **M** (medium): a clear standards violation spread across the code.
- **L** (low): cosmetic or local.

Tick an item when its PR merges. Status after branch `chore/adopt-standards` (2026-11-08): ticked items are done; **Deferred** items say why and where they go next. Per the owner, new tests are deferred to a later pass. The [suggested slices](#suggested-slices-one-branch-and-pr-each) at the end group the items into branches.

---

## 1. Solution and folder structure

- [x] **H: add `Directory.Packages.props`.** Every `PackageReference` is versionless (for example `src/ExpenseTracker.Api/ExpenseTracker.Api.csproj` and `tests/Directory.Build.props`), and `ExpenseTracker.slnx` lists the file, but it isn't on disk or in git. Without it, a clean clone can't restore (NU1015).
- [x] **H: fix CI.** In `.github/workflows/ci.yml`, `WEB_DIR` is still the template placeholder `src/web/<product>-web`. The backend tests also need LocalDB, which `ubuntu-latest` doesn't have. *Done:* the web path and the npm script are fixed. Integration tests are skipped in CI until the SQLite slice (they need LocalDB).
- [x] **H: fix the docs that CLAUDE.md depends on.**
  - [x] Add `docs/spec.md`. It's missing, but `CLAUDE.md` imports it.
  - [x] Replace the template placeholders in `CLAUDE.md` (`<Product>`, `<frontend>`, `<auth>`, `src/web/<product>-web`).
  - [x] Rename `docs/ARCHITECTURE.md` to `docs/architecture.md`. CLAUDE.md imports the lower-case name, which breaks on case-sensitive file systems.
  - [x] Add a `docs/adr/` folder for architecture decision records.
- [x] **M: move the frontend** from `src/ExpenseTracker.Web` to `src/web/expense-tracker-web`, and update the AppHost `AddViteApp` path, CI, README and the CLAUDE.md commands.
- [ ] **M: organise Application as vertical slices**, for example `Application/Expenses/CreateExpense/{CreateExpense,CreateExpenseHandler,CreateExpenseValidator}.cs`. Today `Features/Expenses/ExpenseService.cs` holds 8 use cases. **Deferred** to the family-groups slice (`roadmap.md` 2), which rewrites these use cases anyway.
- [ ] **M: make AppHost start the local dependencies.** It runs only the API and Vite, with no SQL Server or Redis container. The API needs LocalDB, which is Windows-only. *Database part done:* SQLite needs no container. **Still deferred:** Redis needs new packages, which need approval.
- [x] **L: align the test project names** with `ExpenseTracker.UnitTests` and `ExpenseTracker.IntegrationTests`, or keep the split by layer (`Domain.Tests`, `Application.Tests`, `ArchitectureTests`) and record that choice in an ADR. *Done:* the split by layer is kept and recorded in ADR 0004.
- [x] **L: fix the README.** It says "warnings as errors", but `Directory.Build.props` sets `TreatWarningsAsErrors=false`, which is what the standard asks for.

## 2. Naming

- [ ] **M: one public type per file, named after the type.** Files that break this rule: *Done* everywhere except `Application/Features/*`, which is **deferred** to the vertical-slice work.
  - [x] Domain: `Common/Result.cs` (`ErrorKind`, `Error`, `Result`, `Result<T>`), `Budgets/BudgetPolicy.cs` (+`BudgetState`), `Expenses/Expense.cs` (+`ExpenseDetails`)
  - [ ] Application: `Abstractions/Persistence/Repositories.cs`, `Abstractions/Queries/ReadModels.cs`, `Abstractions/Realtime/IChangeNotifier.cs`, `Features/Budgets/BudgetService.cs`, `Features/*/*Models.cs`, `Features/Identity/IdentityServices.cs`, `Security/AccessPolicy.cs` (+`Actor`) *Ports, common and security are done; `Features/*` is deferred.*
  - [x] Infrastructure: `Persistence/Repositories/Repositories.cs`, `Persistence/Queries/CategoryAndBudgetQueries.cs`, `Identity/IdentitySettings.cs`, `Identity/AuthService.cs` (+`RefreshTokenRevocation`)
  - [x] Api: `Auth/JwtOptions.cs` (+`JwtAccessTokenIssuer`), `Configuration/AppOptions.cs`, `Contracts/*.cs`, `Endpoints/CategoryAndBudgetEndpoints.cs`
  - [x] Web: `features/admin/components/badges.tsx` (several components in one lower-case file)
- [x] **M: version the routes.** *Done in `feature/api-v1-routes`:* `/api/v1`, with the refresh cookie on `/api/v1/auth` (ADR 0006). The auth flow is otherwise unchanged.
- [ ] **M: name commands and handlers per use case.** `ExpenseCommand` and `CategoryCommand` serve both create and update; use `CreateExpense`/`UpdateExpense` with `CreateExpenseHandler`, not `*Service`. **Deferred** with the vertical slices.
- [ ] **M: rename tests to `Method_State_ExpectedResult`.** For example `Create_rejects_a_duplicate_name` becomes `CreateAsync_DuplicateName_ReturnsValidationError`. Test classes should be `<Subject>Tests`; today they're `ApiTests`, `AuthTests`, `CategoryAndBudgetServiceTests` and `LayeringTests`. **Deferred:** tests come later. The one new test already follows the pattern.
- [x] **M: give Options classes the `Options` suffix.** Rename `CorsSettings` to `CorsPolicyOptions` (to avoid clashing with ASP.NET's `CorsOptions`), `IdentitySettings` to `IdentityStoreOptions` (to avoid clashing with ASP.NET's `IdentityOptions`) and `IdentitySeedSettings` to `IdentitySeedOptions`.
- [ ] **L: use the right contract suffixes.** `ExpenseListQuery`, `ExpenseExportQuery` and `SummaryQuery` should end in `Request`, and responses should be `*Response` records rather than Application DTOs. **Deferred** to the `api-v1-contracts` slice: renaming `*Query` to `*Request` would collide with the Application types of the same name.
- [x] **L: drop abbreviations.** The parameters `ct` (everywhere) and `db`, and single-letter parameters such as `ToDto(Expense e, Category c)`. *Done* for `ct` and `db`. Single-letter lambda parameters are kept, as is idiomatic.
- [x] **L: prefix booleans** with `Is`, `Has`, `Can` or `Should`: `FeatureOptions.ApiDocs`, `MigrateDatabaseOnStartup`, `IdentitySeedSettings.DemoUsers`.
- [x] **L: fix the cache key format.** `user-access:{guid:N}` (in `UserAccessProvider.cs`) should be `expensetracker:user-access:{id}:v1`.

## 3. Layering and loose coupling

- [ ] **H: add an `ICurrentUser` port and data ownership.** **Moved** to `roadmap.md` slice 2 (family groups): the owner chose per-family data.
  - `Expense` and `Category` have no owner, so every signed-in user can read and edit everyone's data. That's the IDOR risk `security.md` warns about.
  - `SignalRChangeNotifier` broadcasts to `Clients.All`.
  - *Open question:* should data be per user or shared, as a household ledger? If shared, record the decision in an ADR instead.
- [ ] **H: move the identity use cases into Application.** **Deferred, needs a decision:** this rewrites the authentication and user-admin code, and you asked me to leave the RBAC and JWT flow unchanged.
  - `AuthService` (233 lines), `UserAdminService` (395), `RoleAdminService` and `ApiKeyService` are in `Infrastructure/Identity`, and they hold business orchestration: rank checks, session revocation and key limits.
  - Put that orchestration behind narrow ports (user store, refresh-token store, API-key store) and keep `UserManager` and EF Core in the adapters.
- [x] **M: bind every setting with Options and validate on start.** Only `JwtOptions` does this today:
  - `FeatureOptions` is read with `.Get<>()` in `Program.cs`.
  - `CorsSettings` is read inline twice.
  - `IdentitySettings` isn't validated.
  - The connection string is checked by hand, and `AddInfrastructure(configuration, environmentName)` takes the environment name only for an error message.
- [ ] **M: choose adapters by configuration.** *Database:* SQLite replaced SQL Server, behind the same ports; a provider switch comes with MongoDB (ADR 0001). **Still deferred:** Redis needs package approval.
  - SQL Server is hard-wired.
  - `AddHybridCache()` has no Redis L2.
  - There's no `Cache:Provider` (or similar) switch.
- [ ] **M: map API responses explicitly.** Endpoints return Application DTOs (`ExpenseDto`, `ExpensePage`). `BudgetStatusDto.State` is a lower-case string built in Application; it should be an enum serialised as a string. *Partly done:* `BudgetStatusDto.State` is now an enum, with the JSON unchanged. `*Response` mapping is **deferred** to `api-v1-contracts`.
- [ ] **M: generate the frontend API types from OpenAPI.** Today `features/*/model/types.ts` copies the backend DTOs by hand. **Deferred:** needs an npm package (for example `openapi-typescript`), which needs approval.
- [x] **L: settle where the SignalR adapter lives.** The standard puts it in Infrastructure and names the port `INotificationPublisher`. This repo keeps `IChangeNotifier` in Api on purpose. Move it, or write an ADR. *Done:* ADR 0002.
- [ ] **L: pass the origin client ID explicitly.** `SignalRChangeNotifier` currently reads `X-Client-Id` through `IHttpContextAccessor`. **Deferred** to family groups, which changes the notifier for per-family SignalR groups.
- [x] **L: reference other aggregates by ID only.** `Category.Expenses` and `Expense.Category` are object navigations between aggregates. *Done:* kept for now, ADR 0005.
- [x] **L: decide on domain events.** Budget overruns are currently detected by the application service after saving. Use domain events, or record the choice in an ADR. *Done:* ADR 0005.
- [ ] **L: remove the ports' dependency on feature models.** `Abstractions/Identity/IdentityPorts.cs` imports `Features.Identity` types. **Deferred** with the vertical slices.

## 4. Code-level practices

- [x] **H: enable TypeScript strict mode** (`"strict": true` in `src/ExpenseTracker.Web/tsconfig.app.json`) and fix the errors it reports. *Done:* TypeScript 6 already defaulted to strict, so it's now explicit and no code changed.
- [x] **H: add security headers.** CSP, HSTS (`UseHsts`), `X-Content-Type-Options` and `Referrer-Policy` aren't set anywhere. *Done* for the API, with HSTS outside Development. The web app's CSP belongs to whatever hosts the static files (Stage 2 deploy).
- [ ] **M: add idempotency keys to the retriable POSTs:** create expense or category, create user, create API key, bulk delete. **Deferred** until after the SQLite slice: it needs a new table, and the migrations are reset there.
- [x] **M: remove magic numbers.** Use constants or Options for these:
  - [x] `Api/Contracts/ExpenseContracts.cs` and the category contracts repeat `200`, `1000` and `"999999999"` instead of using the domain constants.
  - [x] Password length `10` and lockout `5`/`15` minutes are inline in `Infrastructure/DependencyInjection.cs`, and the `15` is repeated in `AuthService.cs:184`.
  - [x] The cache TTL of 10 minutes, the rate-limit window of 1 minute and the JWT `ClockSkew` of 30 seconds are inline.
  - [x] The text "up to 10,000 rows" in `ExpenseEndpoints` duplicates `ExpenseService.MaxExportRows`.
- [x] **M: make user creation atomic.** `UserAdminService.CreateAsync` creates the user, then adds roles, and deletes the user again if that fails. Use a transaction instead.
- [ ] **M: shorten long methods** to under 30 lines. For example `UserAdminService.ListAsync` is about 45 lines, and `UserAdminService.cs` is 395 lines. *Partly done:* `ListAsync` is split. The rest of `UserAdminService` is **deferred** with the identity decision above.
- [ ] **M: make time handling consistent.** *`CreatedAt` done:* `Expense.CreatedAt` is now a `DateTimeOffset`, like the identity entities. **Still deferred:** The meaning of "current month" depends on a family time zone (family groups).
  - `Expense.CreatedAt` is a `DateTime`, while the identity entities use `DateTimeOffset`.
  - `BudgetService.GetOverviewAsync` and the CSV filename use `GetLocalNow()`, so the server's time zone decides the "current month".
- [x] **L: decide on the frontend libraries.** The code uses hand-rolled validation instead of React Hook Form with a schema, TanStack Table instead of AG Grid, its own toasts instead of Sonner, and oxlint instead of ESLint and Prettier. Switching adds dependencies, so record the choice in an ADR or approve the switch. *Done:* ADR 0003. It also records three toast differences, left unchanged to keep the look and feel.
- [ ] **L: stop hiding missing XML docs.** The global `NoWarn CS1591` hides missing docs on public APIs. **Kept:** the suppression is commented in `Directory.Build.props`. Public APIs get docs as they're touched.
- [x] **L: make startup work cancellable.** `Program.cs` runs migration and seeding without a cancellation token; pass `app.Lifetime.ApplicationStopping`.
- [ ] **L: stream CSV exports** instead of building up to 10,000 rows in a `StringBuilder`. **Won't do for now** (YAGNI): exports are capped at 10,000 rows.

## 5. Missing tests

All **deferred** to a later pass, per the owner. The isolation item moves to the SQLite slice. One test was added for the new security headers.

- [x] **H: isolate the integration tests.** `ApiFactory` shares one LocalDB database across tests. Move to Testcontainers (SQL Server) and reset data with Respawn. *Done in `chore/sqlite-guid-ids`:* a fresh SQLite file per test class, no Docker, and CI runs them on Linux.
- [ ] **H: add unauthenticated and unauthorised tests for every endpoint.** Today only 3 GETs are checked. Missing:
  - [ ] expense PUT and DELETE, bulk delete, export
  - [ ] category CRUD, budgets
  - [ ] each admin endpoint
  - [ ] the `/hubs/expenses` hub
- [ ] **H: unit-test the identity use cases:** refresh-token reuse detection, the user and role admin rules, API key limits and expiry, and `UserAccessProvider` caching and invalidation. This depends on moving them into Application (section 3).
- [ ] **M: cover the remaining Application paths.**
  - [ ] `CategoryService`: update (not found, a duplicate name other than its own, success), create success, sort validation
  - [ ] `ExpenseService`: `GetAsync`, `SummarizeAsync` with an inverted range, update with an unknown ID or category, budget checks across months
  - [ ] `BudgetService.DetectOverrunAsync` with `addedAmount <= 0`
  - [ ] `Paging.Resolve` clamping and page-size limits
  - [ ] Use `FakeTimeProvider` where time matters
- [ ] **M: cover the remaining domain rules.**
  - [ ] `Category`: invalid colour, name too long, negative or too-large budget, `Update`
  - [ ] `Expense`: amount limits, more than 2 decimal places, `CategoryId <= 0`, notes length
  - [ ] `BudgetPolicy`: `StateOf` at its boundaries (exactly 80%, exactly at budget, zero budget), `PercentUsed` with a zero budget, `CrossedLimit`
- [ ] **M: unit-test these Api pieces:**
  - [ ] `ExpenseCsvWriter.Escape` (formula injection, quoting)
  - [ ] `ResultHttpExtensions`, which maps each `ErrorKind` to a status code
  - [ ] `GlobalExceptionHandler`: a 500 returns problem details without leaking details
  - [ ] `ApiKeyAuthenticationHandler`
- [ ] **M: expand frontend tests.** There are 8 test files for 124 source files. Add MSW, then cover:
  - [ ] `shared/api/httpClient` (single-flight refresh on 401) and `shared/auth/session`
  - [ ] the admin feature
  - [ ] the auth pages and ViewModels
  - [ ] budgets, the dashboard, `RealtimeQuerySync`
  - [ ] `ThemeProvider` (Light/Dark/System)
  - [ ] `ExpenseForm` and `ExpenseFilters`
- [ ] **L: extend the architecture tests** to check that endpoint signatures don't expose Domain entities, that Options classes end in `Options`, and that interfaces start with `I`.
- [ ] **L: enforce the coverage target** of 80% for Domain and Application in CI.

---

## Suggested slices (one branch and PR each)

- [x] 1. `chore/restore-build-and-ci`: add `Directory.Packages.props`, fix CI `WEB_DIR`, fill in the CLAUDE.md placeholders, rename `docs/ARCHITECTURE.md`, add `docs/spec.md` and `docs/adr/`.
- [ ] 2. ~~`chore/testcontainers-sql`~~ Replaced by the SQLite slice (`roadmap.md` 1, ADR 0001): no Docker is needed, and each test class gets its own database file.
- [ ] 3. `feature/<n>-data-ownership`: the `ICurrentUser` port, an owner ID and migration, per-user SignalR groups, IDOR tests. Changes the public contract, so it needs a decision first.
- [x] 4. `fix/security-headers-and-ts-strict`: the security headers and TypeScript strict mode.
- [x] 5. `refactor/options-and-constants`: Options naming and validation on start, and removing the magic values.
- [ ] 6. `refactor/identity-use-cases-to-application`: move the identity use cases into Application, with unit tests.
- [ ] 7. `refactor/vertical-slices-and-one-type-per-file`: split the files and rename commands and handlers.
- [ ] 8. `feature/<n>-api-v1-contracts`: `/api/v1`, Request and Response mapping, a generated frontend client, idempotency keys. Changes the public contract.
- [ ] 9. `test/coverage-gaps`: the remaining tests, the test renames and the coverage threshold.
- [x] 10. ADRs for the deliberate deviations: SignalR in Api, the frontend libraries, the test project layout, domain events.

## Definition of done for each slice

- [ ] `dotnet build -c Release` adds no new warnings, and `dotnet format --verify-no-changes` is clean.
- [ ] Frontend lint and tests pass.
- [ ] `dotnet test` passes (with Docker running, after slice 2).
- [ ] `aspire run` works end to end in light and dark themes.
- [ ] CI is green on the PR.
