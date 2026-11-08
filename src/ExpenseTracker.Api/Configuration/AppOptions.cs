namespace ExpenseTracker.Api.Configuration;

/// <summary>Environment-specific switches, bound from the <c>Features</c> section.</summary>
public sealed class FeatureOptions
{
    public const string Section = "Features";

    /// <summary>Serve the OpenAPI document and the Scalar API reference.</summary>
    public bool ApiDocs { get; init; }

    /// <summary>Apply pending EF Core migrations when the app starts.</summary>
    public bool MigrateDatabaseOnStartup { get; init; }

    /// <summary>Seed the built-in roles, their default permissions and the bootstrap administrator (idempotent).</summary>
    public bool SeedIdentityOnStartup { get; init; } = true;
}

/// <summary>Browser origins allowed to call the API directly, bound from the <c>Cors</c> section.</summary>
public sealed class CorsSettings
{
    public const string Section = "Cors";
    public const string PolicyName = "client";

    public string[] AllowedOrigins { get; init; } = [];
}
