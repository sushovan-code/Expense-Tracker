namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>The details of an account to create (always active, email confirmed).</summary>
public sealed record NewUserAccount(string Email, string DisplayName, DateTimeOffset CreatedAt);
