namespace ExpenseTracker.Api.Configuration;

/// <summary>Environment-specific switches, bound from the <c>Features</c> section.</summary>
public sealed class FeatureOptions
{
    public const string Section = "Features";

    /// <summary>Serve the OpenAPI document and the Scalar API reference.</summary>
    [ConfigurationKeyName("ApiDocs")]
    public bool IsApiDocsEnabled { get; init; }

    /// <summary>Apply pending EF Core migrations when the app starts.</summary>
    [ConfigurationKeyName("MigrateDatabaseOnStartup")]
    public bool ShouldMigrateDatabaseOnStartup { get; init; }

    /// <summary>Seed the built-in roles, their default permissions and the bootstrap administrator (idempotent).</summary>
    [ConfigurationKeyName("SeedIdentityOnStartup")]
    public bool ShouldSeedIdentityOnStartup { get; init; } = true;
}
