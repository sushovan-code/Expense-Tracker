# 0005. Aggregate references and side effects without domain events

- Status: Accepted
- Date: 2026-11-08

## Context
Tactical DDD suggests that aggregates reference each other by ID only, and that side effects run from domain events. In this repo:
- `Expense.Category` and `Category.Expenses` are EF navigations between aggregates.
- After saving, the application services call `IChangeNotifier` and `IBudgetMonitor` directly. The budget-overrun alert is one example.

## Decision
Keep both for now.
- The navigations are used only for read projections and EF relationships. Use cases load and change one aggregate at a time, through repositories, and never write through a navigation.
- There are two side effects (the change broadcast and the budget alert), both in the same process and both best-effort. Domain events would add a dispatcher and handlers without removing any real coupling.

## Consequences
- The code stays simple, and the flow is visible in each use case.
- Revisit when side effects multiply or must be reliable, for example the activity log and the 75/90/100% budget alerts in `roadmap.md` (4.1, 4.8). Then raise domain events from the aggregates, and use an outbox if delivery must survive a crash. Moving to MongoDB (ADR 0001) also favours ID-only references.
