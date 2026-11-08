namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>
/// A stored personal API key: only the key's hash is kept, and <see cref="Prefix"/> identifies it in lists.
/// <see cref="Scopes"/> limits the key to some permissions; empty means all of the owner's permissions.
/// </summary>
public sealed record ApiKeyRecord(
    Guid Id,
    Guid UserId,
    string Name,
    string Prefix,
    string KeyHash,
    IReadOnlyList<string> Scopes,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);
