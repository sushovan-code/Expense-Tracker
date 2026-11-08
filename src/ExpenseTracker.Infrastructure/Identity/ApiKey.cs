using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>A personal API key. Only a SHA-256 hash of the key is stored; <see cref="Prefix"/> identifies it in lists.</summary>
public sealed class ApiKey
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public string Prefix { get; set; } = "";
    public string KeyHash { get; set; } = "";
    /// <summary>Space-separated permissions the key is limited to; empty means all of the owner's permissions.</summary>
    public string Scopes { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
