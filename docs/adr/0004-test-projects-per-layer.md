# 0004. One test project per layer

- Status: Accepted
- Date: 2026-11-08

## Context
`docs/standards/naming-conventions.md` shows `tests/<Product>.UnitTests` and `tests/<Product>.IntegrationTests`. This repo has four test projects:
- `ExpenseTracker.Domain.Tests`
- `ExpenseTracker.Application.Tests`
- `ExpenseTracker.ArchitectureTests`
- `ExpenseTracker.Api.IntegrationTests`

## Decision
Keep one test project per layer, plus the architecture tests.
- Each unit-test project references only the layer it tests. So `Domain.Tests` can't use EF Core or ASP.NET Core by accident, and the project references themselves prove the layers stay separate.
- `ArchitectureTests` loads every assembly and stays apart from both the unit tests and the integration tests.

## Consequences
- The unit tests are fast and isolated. `dotnet test --solution ExpenseTracker.slnx` runs everything, and CI can choose projects (as it does until the SQLite slice).
- A new layer or module gets its own `<Layer>.Tests` project.
