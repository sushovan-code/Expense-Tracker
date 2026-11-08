# Coding standards, best practices and design principles

Code is written for the next reader. Prefer clear and boring over clever. These rules apply to every change, and the analyzers in `Directory.Build.props` and `.editorconfig` enforce what they can.

## Design principles
- **SOLID**
  - Single responsibility: one reason to change per class; handlers do one use case.
  - Open/closed: extend through new adapters, strategies or decorators, not by editing stable code.
  - Liskov: implementations honour the port's contract; no `NotImplementedException` in shipped code.
  - Interface segregation: small, focused ports (`IFileStorage`, not `IInfrastructureService`).
  - Dependency inversion: Application depends on ports; Infrastructure implements them (see `architecture.md`).
- **Separation of concerns**: domain rules in Domain, orchestration in Application, I/O in Infrastructure, HTTP in Api.
- **DRY, KISS, YAGNI**: remove real duplication of knowledge, keep solutions simple, build only what the spec needs now.
- **Composition over inheritance**; inheritance only for true is-a relationships, at most one level deep in app code.
- **Fail fast**: validate at the boundary, guard clauses at the top of methods, no silent catch blocks.
- **Immutability by default**: `record` for DTOs, value objects and messages; `init` setters; read-only collections in public APIs.
- **Tactical DDD where the domain is rich** (Meridian, Claims, Loan, AI-SDLC): aggregates guard their invariants, value objects for money, IDs and dates, domain events for side effects.
- **Design patterns only when they solve a present problem**: Strategy (pluggable providers, assignment rules), Decorator (caching, retry, logging around a port), Factory (provider selection), Specification (complex queries), Outbox and Saga (messaging). Name the pattern in the class or ADR when you use one.

## C# and .NET practices
- Nullable reference types enabled; `AnalysisLevel` latest-recommended. Warnings don't fail the build (`TreatWarningsAsErrors` is false), but fix the ones your change introduces, especially nullable and async warnings. Don't silence them with `#pragma` or `NoWarn` without a comment explaining why.
- Small methods (aim under 30 lines), early returns, no nesting deeper than 3 levels; no magic numbers or strings (constants or Options).
- Async all the way: `async`/`await` end to end, every async method takes a `CancellationToken` and passes it on; never `.Result`, `.Wait()` or `async void` (except event handlers).
- Expected failures (validation, not found, business-rule breaches) return a `Result`/`Result<T>` and map to Problem Details; exceptions are for unexpected faults only. Never use exceptions for control flow.
- Dependency injection through constructors (primary constructors are fine); correct lifetimes (no scoped service captured by a singleton); no service locator, no static mutable state.
- Use `TimeProvider` for time, `IOptions<T>` for settings, `ILogger<T>` with message templates (no string interpolation in log calls).
- `HttpClient` only through `IHttpClientFactory` / typed clients with standard resilience (`AddStandardResilienceHandler`).
- EF Core: `AsNoTracking` for reads, project to DTOs with `Select`, avoid N+1 (`Include` deliberately or split queries), always paginate lists, migrations reviewed in the PR.
- APIs: Minimal API endpoint groups per feature, typed results, Problem Details for errors, versioned routes, idempotency keys on write endpoints that can be retried.
- Prefer framework or well-maintained OSS libraries with clear licences; check the licence before adding one (some popular .NET libraries have moved to commercial licences).
- Public APIs and non-obvious code get XML doc comments; comments explain why, not what.

## TypeScript, Angular and React practices
- TypeScript `strict: true`; no `any` (use `unknown` and narrow); ESLint and Prettier must pass.
- Shared API types generated from the backend OpenAPI spec; no hand-copied DTOs.
- **Angular**: standalone components, signals for state, `inject()`, `OnPush` change detection, smart (container) vs presentational components, lazy-loaded feature routes, `takeUntilDestroyed` or the `async` pipe for subscriptions, typed reactive forms.
- **React**: function components and hooks only, follow the Rules of Hooks, TanStack Query for server state, local state close to where it is used, context for app-wide concerns (theme, user, notifications), memoisation only where profiling shows a need, forms with React Hook Form and schema validation.
- Components are accessible by default: semantic HTML first, labels on every control, keyboard support.

## Code review checklist
- [ ] Names follow `naming-conventions.md` and read as business language
- [ ] Each class and method has one clear responsibility; no layer rule broken
- [ ] Inputs validated; errors handled and surfaced as Problem Details or a clear UI message
- [ ] Async code passes `CancellationToken`; no blocking calls
- [ ] No duplication, dead code, commented-out code, TODOs without an issue link, or magic values
- [ ] Tests cover the behaviour and edge cases (see `testing.md`)
- [ ] Security and logging rules followed (see `security.md`)
- [ ] UI works in light, dark and system themes (see `ui-and-theming.md`)
