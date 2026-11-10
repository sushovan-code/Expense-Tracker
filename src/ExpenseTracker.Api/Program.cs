using ExpenseTracker.Api;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Middleware;
using ExpenseTracker.Application;
using ExpenseTracker.Infrastructure;
using ExpenseTracker.Infrastructure.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
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
    .AddInfrastructure(builder.Configuration, builder.Environment.EnvironmentName, builder.Environment.ContentRootPath)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

var features = app.Services.GetRequiredService<IOptions<FeatureOptions>>().Value;
app.LogStartupConfiguration(features);

// Outermost, so it records the final status code (including 500s produced by the exception handler).
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    // Browsers only honour HSTS over HTTPS; Development keeps plain HTTP for the local ports.
    app.UseHsts();
}

app.MapDefaultEndpoints();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (features.IsApiDocsEnabled)
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
    // Stops waiting on the database if the host is shut down during startup.
    var stopping = app.Lifetime.ApplicationStopping;

    if (features.ShouldMigrateDatabaseOnStartup)
    {
        await scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().MigrateAsync(stopping);
    }

    // Built-in roles and their default permissions, plus the bootstrap administrator when configured.
    if (features.ShouldSeedIdentityOnStartup)
    {
        await scope.ServiceProvider.GetRequiredService<IdentitySeeder>().SeedAsync(stopping);
    }
}

app.UseCors(CorsPolicyOptions.PolicyName);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<FamilyContextMiddleware>(); // after the existing auth: reads the caller's family, changes nothing
app.MapApiEndpoints();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program;
