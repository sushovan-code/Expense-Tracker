using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Application.Common;
using Microsoft.AspNetCore.SignalR;

namespace ExpenseTracker.Api.Realtime;

/// <summary>
/// Push-only hub: clients listen for change notifications; all writes still go through the REST API
/// (validation, logging and problem details stay in one place).
/// </summary>
public sealed partial class ExpenseHub(PresenceTracker presence, ICurrentFamily family, ILogger<ExpenseHub> logger) : Hub<IExpenseHubClient>
{
    public const string Path = "/hubs/expenses";

    public override async Task OnConnectedAsync()
    {
        var online = presence.Add(Context.ConnectionId);
        LogClientConnected(logger, Context.ConnectionId, ClientId, online);
        // Each connection listens only to its own family; someone without a family hears nothing.
        if (family.FamilyId is { } familyId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, FamilyGroup.Name(familyId));
            await Clients.Group(FamilyGroup.Name(familyId)).PresenceChanged(new PresenceUpdate(online));
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var online = presence.Remove(Context.ConnectionId);
        LogClientDisconnected(logger, exception, Context.ConnectionId, ClientId, online);
        if (family.FamilyId is { } familyId)
        {
            await Clients.OthersInGroup(FamilyGroup.Name(familyId)).PresenceChanged(new PresenceUpdate(online));
        }
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
