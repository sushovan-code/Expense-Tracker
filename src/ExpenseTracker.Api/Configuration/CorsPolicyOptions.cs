namespace ExpenseTracker.Api.Configuration;

/// <summary>
/// Browser origins allowed to call the API directly, bound from the <c>Cors</c> section.
/// (Named <c>CorsPolicyOptions</c> so it doesn't clash with ASP.NET Core's <c>CorsOptions</c>.)
/// </summary>
public sealed class CorsPolicyOptions
{
    public const string Section = "Cors";
    public const string PolicyName = "client";

    public string[] AllowedOrigins { get; init; } = [];
}
