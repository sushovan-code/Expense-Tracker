using ExpenseTracker.Application.Abstractions.Realtime;

namespace ExpenseTracker.Api.Realtime;

public interface IExpenseHubClient
{
    Task ExpensesChanged(ChangeNotice notice);
    Task CategoriesChanged(ChangeNotice notice);
    Task BudgetExceeded(BudgetAlert alert);
    Task PresenceChanged(PresenceUpdate update);
}
