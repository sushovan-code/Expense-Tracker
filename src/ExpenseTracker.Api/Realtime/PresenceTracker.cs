using System.Collections.Concurrent;

namespace ExpenseTracker.Api.Realtime;

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
