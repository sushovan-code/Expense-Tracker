using System.Collections.Concurrent;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace ExpenseTracker.Api.Realtime;

public interface IExpenseHubClient
{
    Task ExpensesChanged(ChangeNotice notice);
    Task CategoriesChanged(ChangeNotice notice);
    Task BudgetExceeded(BudgetAlert alert);
    Task PresenceChanged(PresenceUpdate update);
}
