using System.Collections.Concurrent;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace ExpenseTracker.Api.Realtime;

// ---- Wire contracts (server → client). Method names are the client-side event names. ----

/// <param name="Action">created | updated | deleted</param>
/// <param name="OriginClientId">The browser tab that caused the change (X-Client-Id), so it can skip its own echo.</param>
public sealed record ChangeNotice(string Action, IReadOnlyCollection<int> Ids, string? OriginClientId);

public sealed record PresenceUpdate(int Online);

public interface IExpenseHubClient
{
    Task ExpensesChanged(ChangeNotice notice);
    Task CategoriesChanged(ChangeNotice notice);
    Task BudgetExceeded(BudgetAlert alert);
    Task PresenceChanged(PresenceUpdate update);
}

/// <summary>Tracks open connections so every client can show how many sessions are live.</summary>
public sealed class PresenceTracker
{
    private readonly ConcurrentDictionary<string, byte> _connections = new();

    public int Add(string connectionId)
    {
        _connections.TryAdd(connectionId, 0);
        return _connections.Count;
    }

    public int Remove(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        return _connections.Count;
    }
}

/// <summary>
/// Push-only hub: clients listen for change notifications; all writes still go through the REST API
/// (validation, logging and problem details stay in one place).
/// </summary>
public sealed partial class ExpenseHub(PresenceTracker presence, ILogger<ExpenseHub> logger) : Hub<IExpenseHubClient>
{
    public const string Path = "/hubs/expenses";

    public override async Task OnConnectedAsync()
    {
        var online = presence.Add(Context.ConnectionId);
        LogClientConnected(logger, Context.ConnectionId, ClientId, online);
        await Clients.All.PresenceChanged(new PresenceUpdate(online));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var online = presence.Remove(Context.ConnectionId);
        LogClientDisconnected(logger, exception, Context.ConnectionId, ClientId, online);
        await Clients.Others.PresenceChanged(new PresenceUpdate(online));
        await base.OnDisconnectedAsync(exception);
    }

    private string? ClientId => Context.GetHttpContext()?.Request.Query["clientId"].ToString() is { Length: > 0 } id ? id : null;

    [LoggerMessage(EventId = LogEvents.ClientConnected, Level = LogLevel.Information,
        Message = "Realtime client connected: connection {ConnectionId}, client {ClientId}; {Online} online")]
    private static partial void LogClientConnected(ILogger logger, string connectionId, string? clientId, int online);

    [LoggerMessage(EventId = LogEvents.ClientDisconnected, Level = LogLevel.Information,
        Message = "Realtime client disconnected: connection {ConnectionId}, client {ClientId}; {Online} online")]
    private static partial void LogClientDisconnected(ILogger logger, Exception? exception, string connectionId, string? clientId, int online);
}
