using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// Startup seeding, bound from <c>Identity:Seed</c>. Roles and their default permissions are always seeded.
/// The bootstrap SuperAdmin is created only when <see cref="AdminEmail"/> and <see cref="AdminPassword"/> are
/// set: supply the password through user secrets or an environment variable (Identity__Seed__AdminPassword),
/// never a committed file outside Development.
/// </summary>
public sealed class IdentitySeedOptions
{
    public string? AdminEmail { get; init; }
    public string? AdminPassword { get; init; }
    public string AdminDisplayName { get; init; } = "Administrator";

    /// <summary>Development only: also create one demo user per lower role (admin@, user@, reader@ on the admin's domain).</summary>
    [ConfigurationKeyName("DemoUsers")]
    public bool ShouldSeedDemoUsers { get; init; }

    public string? DemoPassword { get; init; }
}
