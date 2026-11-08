namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Whether an account is locked out after failed sign-ins, and until when.</summary>
public sealed record LockoutStatus(bool IsLockedOut, DateTimeOffset? End);
