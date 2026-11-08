# Architecture

Expense Tracker is split into independent layers on both sides. Each layer depends only on the layers inside it, and those rules are enforced by tests and lint, not just convention.

```
┌──────────────────────────── Browser ────────────────────────────┐      ┌──────────────────────────── Server ─────────────────────────────┐
│  app/        composition root: providers, DI wiring, routes     │      │  ExpenseTracker.Api            presentation + composition root  │
│     ↓                                                           │ HTTP │     ↓                     ↓                                     │
│  features/   expenses · categories · budgets · dashboard        │ ───▶ │  ExpenseTracker.Infrastructure  EF Core, SQLite, queries         │
│     ↓        (View ← ViewModel hook ← API service + Query)      │  WS  │     ↓                                                           │
│  shared/     http client, DI, UI kit, realtime, telemetry       │ ◀─── │  ExpenseTracker.Application    use cases + ports (interfaces)    │
└─────────────────────────────────────────────────────────────────┘      │     ↓                                                           │
                                                                         │  ExpenseTracker.Domain         entities + business rules         │
                                                                         └─────────────────────────────────────────────────────────────────┘
```

## Backend: Clean Architecture

| Project | Responsibility | May depend on |
| --- | --- | --- |
| `ExpenseTracker.Domain` | Entities (`Expense`, `Category`) with app-generated GUID v7 IDs, private setters and factory methods that enforce invariants; error catalogs; `BudgetPolicy`; `Result`/`Error`. | Nothing; plain .NET only |
| `ExpenseTracker.Application` | Use cases (`IExpenseService`, `ICategoryService`, `IBudgetService`) and the **ports** they need: repositories, unit of work, read-side queries, `IChangeNotifier`. DTOs, paging/sorting rules, log event ids. | Domain, plus logging/DI abstractions |
| `ExpenseTracker.Infrastructure` | **Adapters** for the persistence and query ports: EF Core `AppDbContext` (also the unit of work), entity configurations, migrations, repositories, SQL projections, the database health check, SqlClient tracing. | Application, Domain |
| `ExpenseTracker.Api` | Minimal API endpoints, HTTP contracts (validated request records mapped to commands), problem details, middleware, the SignalR hub and its `IChangeNotifier` adapter, configuration. **Composition root.** | All of the above |
| `ExpenseTracker.ServiceDefaults` | Host defaults: OpenTelemetry, health endpoints, service discovery, resilience. | — |
| `ExpenseTracker.AppHost` | Aspire orchestration for local development only. | Api (project reference) |

### Loose-coupling techniques

- **Ports and adapters.** Use cases depend on interfaces (`IExpenseRepository`, `IExpenseQueries`, `IChangeNotifier`, …). EF Core and SignalR are plug-in details; you could swap one without touching a use case.
- **CQRS-style split.** Commands load aggregates through repositories and save them through `IUnitOfWork`. Reads are separate query services that project straight to DTOs.
- **Internal implementations.** Service, repository and query classes are `internal`. The only way in is the interface plus the layer's `AddApplication()`, `AddInfrastructure()` or `AddPresentation()` registration.
- **Errors as values.** Expected failures are returned as `Result`/`Error` (validation, not found, conflict) and mapped to RFC 9457 problem details in one place.
- **Defence in depth.** HTTP contracts validate input for fast 400s and API documentation. The domain enforces the same rules again, so any future entry point (a job, another API) stays safe.

### Enforced by tests (`tests/`)

| Project | What it proves |
| --- | --- |
| `ExpenseTracker.ArchitectureTests` | The dependency rule (Domain → nothing; Application ↛ EF Core or ASP.NET Core; Infrastructure ↛ Api), that use cases and adapters are internal, and that entities have no public setters. |
| `ExpenseTracker.Domain.Tests` | Entity invariants and the budget policy. |
| `ExpenseTracker.Application.Tests` | Use cases in isolation, with NSubstitute fakes for every port. |
| `ExpenseTracker.Api.IntegrationTests` | The real API in memory (WebApplicationFactory) against a throwaway SQLite file per test class that is created by migrations and deleted afterwards. |

## Security: authentication & RBAC

```
request ─▶ JwtOrApiKey policy scheme ──┬─ x-api-key header? ─▶ ApiKey handler ─┐
                                       └─ otherwise ─────────▶ JWT bearer ─────┤
                                                                               ▼
                         IUserAccessProvider (HybridCache): active? security stamp? roles + permissions
                                                                               ▼
              authorization: fallback = signed in · endpoint permission (expense.add …) · "Admin or above"
                                                                               ▼
                                 use case: AccessPolicy (rank rules, no self-management, no escalation)
```

| Concern | Where | How |
| --- | --- | --- |
| Catalog | `Application/Security` | `Permissions` (`resource.action`), `AppRoles` (SuperAdmin 400 > Admin 300 > User 200 > Reader 100, with default grants), `AppClaimTypes`. |
| Anti-escalation rules | `Application/Security/AccessPolicy.cs` | Pure functions, unit-tested: manage only users below your rank (SuperAdmins: anyone), never yourself; assign only roles below your own; grant only permissions you hold; SuperAdmin role is fixed. |
| Identity store | `Infrastructure/Identity` | ASP.NET Core Identity core + EF stores (Guid keys, `auth` schema). Permissions are role claims and (optionally) direct user claims of type `permission`. Lockout: 5 attempts / 15 min. |
| Sessions | `AuthService` | 15-minute JWT access token + 7-day rotating refresh token (HttpOnly, Secure, SameSite=Strict cookie on `/api/v1/auth`, stored hashed). Replaying a rotated token revokes the whole session. |
| API keys | `ApiKeyService`, `ApiKeyAuthenticationHandler` | `etk_…` keys, stored as SHA-256, always expiring, optionally scoped; act as the owner, intersected with scopes. Can't manage credentials (`Interactive` policy). |
| Effective access | `UserAccessProvider` | Roles/permissions are **not** in the token: they're resolved per request from a cached snapshot that every change invalidates, so revocations apply on the next request. A changed security stamp (password change/reset, disable, "sign out everywhere") rejects old tokens. |
| Endpoints | `Api/Auth`, `Endpoints/AuthEndpoints.cs`, `AdminEndpoints.cs` | Deny-by-default fallback policy; `.RequirePermission(...)` via `IAuthorizationRequirementData`; `/api/v1/admin/*` needs Admin or above **and** the specific permission. Rate-limited login/refresh. Audit log events 6xxx. |
| Browser | `shared/auth`, `features/auth`, `features/admin` | Access token in memory only; silent refresh before expiry and on 401 (single-flight, Web Locks across tabs); sign-in/out broadcast to other tabs; route guards and permission-aware UI (cosmetic: the API enforces everything). SignalR connects with the token and is closed when it expires. |

Adding a permission: add it to `Permissions.cs` (and `shared/auth/permissions.ts`), grant it in `AppRoles` defaults, and put `.RequirePermission(...)` on the endpoint. New permissions reach existing Admin/User/Reader roles only through the Roles admin screen (seeding never overwrites edited roles); SuperAdmin always has everything.

## Frontend: MVVM-style React

React has no MVVM framework, but the community's recommended structure maps onto it cleanly:

| MVVM (WPF) | Here | Example |
| --- | --- | --- |
| **Model** | Types, validation and API services in `features/<x>/model` and `features/<x>/api` | `ExpensesApi`, `validateExpense` |
| **ViewModel** | A custom hook per screen. It owns state, derives display data and exposes commands, with no JSX. | `useExpensesViewModel()` |
| **View** | Page and presentational components that only bind to the ViewModel | `ExpensesPage`, `ExpensesGrid`, `ExpenseFilters` |
| **Binding** | React state and props; server state through **TanStack Query** (cache, background refresh, invalidation) | `useExpensesPage(query)` |
| **DI container** | `shared/di`: typed service tokens plus `ServicesProvider`/`useService` | `useService(ExpensesApiToken)` |
| **Messenger / events** | SignalR events → `RealtimeQuerySync` → cache invalidation | another tab edits an expense → every open view refreshes |

### Folder rules

```
src/
├─ app/        composition root: AppProviders, services.ts (DI wiring), queryClient, routes, layout, styles
├─ features/
│  ├─ expenses/    api/ · model/ · viewModels/ · components/ · pages/ · index.ts (public API)
│  ├─ categories/
│  ├─ budgets/
│  └─ dashboard/   composes expenses + budgets via their public APIs
├─ shared/     framework-level building blocks: http client, DI, UI kit, hooks, realtime, telemetry, theme, toasts
└─ test/       test providers (renderWithProviders / renderHookWithProviders)
```

- `shared` may not import `features` or `app`.
- `features` may not import `app`, and may use another feature **only through its `index.ts`**.
- Inside a feature, files import each other relatively.

These rules are lint errors (see `.oxlintrc.json`); `import/no-cycle` forbids circular imports.

### Why it's loosely coupled

- Features never construct their dependencies. `src/app/services.ts` is the only place that binds `ExpensesApiToken` to `createExpensesApi(http)`, so a test or another host can bind a fake instead.
- Mutations invalidate **cache roots** (`QueryRoots` in shared), not other features' internals. For example, an expense change refreshes budgets and category counts without the expenses feature knowing about them.
- Realtime is a cross-cutting concern handled once at the app level. Pages don't know SignalR exists.
- ViewModels are testable without rendering a page or mocking HTTP: inject fake services and assert on state and calls.
