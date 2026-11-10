# 0007. Family isolation as a separate layer

- Status: Accepted
- Date: 2026-11-10

## Context
Expenses and categories were global. Families need their own data, with roles Owner, Member and Viewer, private expenses and invite links. The app-wide RBAC and the JWT flow must not change.

## Decision
- **Separate from RBAC.** The existing app permission still decides whether you may use expenses at all. Family access is an extra check that runs after it. A new `FamilyContextMiddleware` runs after authentication and authorisation, looks up the caller's membership once per request and fills a scoped `FamilyContext` (`ICurrentFamily`). It reads only; roles, claims, the token and `IUserAccessProvider` are untouched, and no family data goes into the token.
- **Isolation in the data layer.** EF Core named global query filters on `Expense` (`Family`, `Private`) and `Category` (`Family`) read that context. A user without a family matches nothing. `ExecuteDelete`, `Count` and `Sum` honour the filters. Another family's ids return 404, so their existence isn't revealed.
- **Rules in the domain.** `FamilyPolicy` holds the role rules as pure functions; use cases call it before writing. Owner manages the family, categories and budgets and changes any shared expense; Member adds and changes their own; Viewer reads. The last Owner can't leave or be demoted.
- **Private expenses** are visible only to their creator (the `Private` filter), the Owner included.
- **Invites** are single-use, expire after 48 hours, and only the SHA-256 hash of the token is stored. Accepting one creates the account with the default app role `User`; family actions never change app roles. The accept endpoint is anonymous and uses the existing auth rate limit.
- **Realtime.** The hub puts each connection in a `family:{id}` group and events are sent to that group only.
- **Guard rails.** Architecture tests require the `Family` filter on every family-owned entity and allow `IgnoreQueryFilters` only in one listed place.

## Consequences
- Every authenticated request costs one small indexed lookup for the membership. A cache can be added later if it shows up in profiles.
- Categories are per family. The old global seed became `DefaultCategories`, copied into each new family.
- Migrations were reset again (synthetic data): delete `src/ExpenseTracker.Api/App_Data/` once after pulling.
- Events about a private expense reach the whole family group as ids only; clients refetch through the filtered API, so the content isn't exposed.
- Not done, to keep this simple: a SaveChanges interceptor that stamps and checks `FamilyId`. Use cases set it from the context and the filters scope every read, update and delete.
- For MongoDB later, the filters become a family id condition inside the adapters; Application code doesn't change.
