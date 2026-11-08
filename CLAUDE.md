# <Product>

<one-line purpose>. Portfolio project; synthetic data only.

## Stack
.NET 10 · <frontend> · <auth> · Aspire · see @docs/architecture.md for services and data stores.

## Commands
- Run everything locally: `aspire run` (AppHost: `src/<Product>.AppHost`)
- Backend tests: `dotnet test`
- Frontend tests: `npm test --prefix src/web/<product>-web`
- Render diagrams: `pwsh -File scripts/render-diagrams.ps1`
- Format: `dotnet format` · `npm run lint --prefix src/web/<product>-web`

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

## Product spec
@docs/spec.md
