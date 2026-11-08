# Expense Tracker

Family expense tracking with categories, monthly budgets and live updates. Portfolio project; synthetic data only.

## Stack
.NET 10 · React + TypeScript (Vite) · ASP.NET Core Identity with JWT bearer and API keys · Aspire · see @docs/architecture.md for services and data stores.

## Commands
- Run everything locally: `aspire run` (AppHost: `src/ExpenseTracker.AppHost`)
- Backend tests: `dotnet test --solution ExpenseTracker.slnx`
- Frontend tests: `npm test --prefix src/ExpenseTracker.Web`
- Render diagrams: `pwsh -File scripts/render-diagrams.ps1`
- Format: `dotnet format ExpenseTracker.slnx` · `npm run lint --prefix src/ExpenseTracker.Web`

## Standards (read the relevant file before working in that area)
- Coding standards, best practices and design principles (always applies): @docs/standards/coding-standards.md
- Development workflow and definition of done: @docs/standards/development-workflow.md
- Naming conventions: @docs/standards/naming-conventions.md
- Architecture and loose coupling: @docs/standards/architecture.md
- Testing: @docs/standards/testing.md
- Security: @docs/standards/security.md
- UI, design and theming: @docs/standards/ui-and-theming.md
- Caching: @docs/standards/caching.md

## Always
- When you add, remove or rewire a service, data store, cache, queue, external system or main flow, update `docs/diagrams/` and README.md's Architecture section (`/update-diagrams`).
- When you add a secret, prerequisite, port or test user, update README.md's "Run it locally" section.
- Don't change the existing look and feel (layout, flows, themes, colours). New UI reuses `shared/ui` components and `app/styles/tokens.css` tokens.
- Don't change the existing RBAC or JWT bearer authentication flow. Family access is a separate layer on top.

## Product spec and plans
@docs/spec.md

Work in progress is tracked in `docs/adoption-plan.md` (standards gaps) and `docs/roadmap.md` (features). Decisions are in `docs/adr/`.
