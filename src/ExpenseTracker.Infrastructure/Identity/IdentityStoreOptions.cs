namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Identity store settings, bound from the <c>Identity</c> section and validated at startup.</summary>
public sealed class IdentityStoreOptions
{
    public const string Section = "Identity";

    /// <summary>How long a refresh token (and so an idle session) lives; each refresh starts a new period (1 hour to 90 days).</summary>
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    public IdentitySeedOptions Seed { get; init; } = new();
}
