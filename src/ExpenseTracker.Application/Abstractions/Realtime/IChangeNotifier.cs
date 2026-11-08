namespace ExpenseTracker.Application.Abstractions.Realtime;

/// <summary>
/// Tells connected clients that data changed. Implemented by the presentation layer (SignalR);
/// implementations must never throw — a failed broadcast must not fail the change itself.
/// </summary>
public interface IChangeNotifier
{
    Task ExpensesChangedAsync(ChangeAction action, IReadOnlyCollection<int> ids, CancellationToken ct);
    Task CategoriesChangedAsync(ChangeAction action, IReadOnlyCollection<int> ids, CancellationToken ct);
    Task BudgetExceededAsync(BudgetAlert alert, CancellationToken ct);
}
