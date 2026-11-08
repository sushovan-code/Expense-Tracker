using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace ExpenseTracker.Api.Realtime;

/// <summary>SignalR adapter for the Application's <see cref="IChangeNotifier"/> port. Never throws.</summary>
internal sealed partial class SignalRChangeNotifier(
    IHubContext<ExpenseHub, IExpenseHubClient> hub,
    IHttpContextAccessor httpContext,
    ILogger<SignalRChangeNotifier> logger) : IChangeNotifier
{
    public const string ClientIdHeader = "X-Client-Id";

    public Task ExpensesChangedAsync(ChangeAction action, IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        SendAsync(nameof(IExpenseHubClient.ExpensesChanged), ids.Count,
            () => hub.Clients.All.ExpensesChanged(new ChangeNotice(ToWire(action), ids, OriginClientId)));

    public Task CategoriesChangedAsync(ChangeAction action, IReadOnlyCollection<int> ids, CancellationToken cancellationToken) =>
        SendAsync(nameof(IExpenseHubClient.CategoriesChanged), ids.Count,
            () => hub.Clients.All.CategoriesChanged(new ChangeNotice(ToWire(action), ids, OriginClientId)));

    public Task BudgetExceededAsync(BudgetAlert alert, CancellationToken cancellationToken) =>
        SendAsync(nameof(IExpenseHubClient.BudgetExceeded), 1, () => hub.Clients.All.BudgetExceeded(alert));

    private static string ToWire(ChangeAction action) => action.ToString().ToLowerInvariant();

    private string? OriginClientId =>
        httpContext.HttpContext?.Request.Headers[ClientIdHeader].ToString() is { Length: > 0 } id ? id : null;

    private async Task SendAsync(string eventName, int itemCount, Func<Task> send)
    {
        try
        {
            await send();
            LogBroadcastSent(logger, eventName, itemCount);
        }
#pragma warning disable CA1031 // A failed broadcast must never fail the change that triggered it.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogBroadcastFailed(logger, ex, eventName);
        }
    }

    [LoggerMessage(EventId = LogEvents.BroadcastSent, Level = LogLevel.Debug,
        Message = "Broadcast {RealtimeEvent} for {ItemCount} item(s)")]
    private static partial void LogBroadcastSent(ILogger logger, string realtimeEvent, int itemCount);

    [LoggerMessage(EventId = LogEvents.BroadcastFailed, Level = LogLevel.Warning,
        Message = "Failed to broadcast {RealtimeEvent}; clients will catch up on their next refresh")]
    private static partial void LogBroadcastFailed(ILogger logger, Exception exception, string realtimeEvent);
}
