using ExpenseTracker.Api;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Middleware;
using ExpenseTracker.Application;
using ExpenseTracker.Infrastructure;
using ExpenseTracker.Infrastructure.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using Scalar.AspNetCore;

// Composition root: the only place that knows every layer.

var builder = WebApplication.CreateBuilder(args);

// Settings live in configs/appsettings.json + configs/appsettings.{Environment}.json.
builder.Configuration.UseConfigsFolder(builder.Environment);

// OpenTelemetry (logs, traces, metrics), health checks, service discovery, HTTP resilience.
// Telemetry is exported over OTLP only when OTEL_EXPORTER_OTLP_ENDPOINT is set (e.g. by the Aspire AppHost).
builder.AddServiceDefaults();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

var features = app.Configuration.GetSection(FeatureOptions.Section).Get<FeatureOptions>() ?? new();
app.LogStartupConfiguration(features);

// Outermost, so it records the final status code (including 500s produced by the exception handler).
app.UseMiddleware<RequestLoggingMiddleware>();
app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (features.ApiDocs)
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference(options => options
        .WithTitle($"Expense Tracker API ({app.Environment.EnvironmentName})")
        .WithTheme(ScalarTheme.Purple)
        .AddPreferredSecuritySchemes("Bearer"))
        .AllowAnonymous();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    if (features.MigrateDatabaseOnStartup)
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().MigrateAsync();

    // Built-in roles and their default permissions, plus the bootstrap administrator when configured.
    if (features.SeedIdentityOnStartup)
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync();
}

app.UseCors(CorsSettings.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapApiEndpoints();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
