# 0002. The SignalR notifier lives in the Api project

- Status: Accepted
- Date: 2026-11-08

## Context
`docs/standards/architecture.md` lists SignalR among the Infrastructure adapters and names the port `INotificationPublisher`. Here the port is `IChangeNotifier` (Application), and its adapter `SignalRChangeNotifier` sits in `ExpenseTracker.Api/Realtime` next to `ExpenseHub`.

## Decision
Keep the adapter in Api, and keep the name `IChangeNotifier`.
- The hub, its authentication (`CloseOnAuthenticationExpiration`, token from the query string) and the notifier all depend on ASP.NET Core SignalR. ASP.NET Core SignalR is a presentation framework that Api already references. Moving only the notifier to Infrastructure would make Infrastructure depend on ASP.NET Core just to reach `IHubContext`.
- The port describes data changes (`ExpensesChangedAsync`, `BudgetExceededAsync`), not general notifications, so `IChangeNotifier` is the more accurate name.

## Consequences
- The dependency rule still holds: use cases see only `IChangeNotifier`, and the architecture tests keep Application free of ASP.NET Core.
- A different transport (Azure SignalR Service, a message bus) is a new adapter registered in its own project. The use cases don't change.
- Revisit this if a second host (for example a background worker) needs to send notifications without the web stack.
