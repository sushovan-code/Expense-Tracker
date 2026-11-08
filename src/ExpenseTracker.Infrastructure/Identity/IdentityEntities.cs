using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity;

// Persistence models for the identity store. They are ASP.NET Core Identity types (not Domain
// entities): the domain is expenses and budgets, identity is a supporting concern behind ports.

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";

    /// <summary>Disabled users can't sign in, and their existing sessions and API keys stop working.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}

public sealed class AppRole : IdentityRole<Guid>
{
    public AppRole() { }

    public AppRole(string name) : base(name) { }

    public string Description { get; set; } = "";
}

/// <summary>
/// One link in a rotating refresh-token chain. Only a SHA-256 hash is stored. Every token of one sign-in
/// shares a <see cref="SessionId"/>, so presenting an already-rotated token can revoke the whole chain.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    /// <summary>Set when the token was rotated (as opposed to revoked by sign-out).</summary>
    public Guid? ReplacedById { get; set; }
    public string? CreatedByIp { get; set; }
}

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
