namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>A user account as the identity use cases see it (no password hash or security stamp).</summary>
public sealed record UserAccount(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset? LockoutEnd,
    int AccessFailedCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt);
