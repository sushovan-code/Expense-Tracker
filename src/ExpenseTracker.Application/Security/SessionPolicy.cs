namespace ExpenseTracker.Application.Security;

/// <summary>Session settings the sign-in use cases need; the host builds it from validated configuration.</summary>
/// <param name="RefreshTokenLifetime">How long a refresh token (and so an idle session) lives; each refresh starts a new period.</param>
public sealed record SessionPolicy(TimeSpan RefreshTokenLifetime);
