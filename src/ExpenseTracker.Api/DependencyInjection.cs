using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Endpoints;
using ExpenseTracker.Api.Middleware;
using ExpenseTracker.Api.Realtime;
using ExpenseTracker.Application.Abstractions.Realtime;
using ExpenseTracker.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using OpenTelemetry.Trace;

namespace ExpenseTracker.Api;

internal static partial class DependencyInjection
{
    /// <summary>HTTP/realtime presentation concerns: problem details, authentication/authorization, OpenAPI, CORS, SignalR and its notifier.</summary>
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidation();
        services.AddProblemDetails(options => options.CustomizeProblemDetails = ProblemDetailsSetup.Customize);
        services.AddExceptionHandler<GlobalExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddSignalR();
        services.AddSingleton<PresenceTracker>();
        services.AddScoped<IChangeNotifier, SignalRChangeNotifier>();
        services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSource("Microsoft.AspNetCore.SignalR.Server"));

        services.AddAuth(configuration);

        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "Expense Tracker API",
                Version = "v1",
                Description = "Track expenses, organise them into categories, set monthly budgets and get spending summaries. "
                    + $"Live change notifications are pushed over SignalR at {ExpenseHub.Path}. "
                    + "Authenticate with a bearer token from POST /api/auth/login, or with a personal API key in the "
                    + $"{AuthSchemes.ApiKeyHeader} header. Each operation requires a permission (e.g. expense.add).",
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[AuthSchemes.Bearer] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token from POST /api/auth/login (valid for 15 minutes).",
            };
            document.Components.SecuritySchemes[AuthSchemes.ApiKey] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = AuthSchemes.ApiKeyHeader,
                Description = "Personal API key from POST /api/auth/api-keys.",
            };
            document.Security =
            [
                new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(AuthSchemes.Bearer, document)] = [] },
                new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(AuthSchemes.ApiKey, document)] = [] },
            ];
            return Task.CompletedTask;
        }));

        services.AddOptions<FeatureOptions>().Bind(configuration.GetSection(FeatureOptions.Section)).ValidateOnStart();
        services.AddOptions<CorsPolicyOptions>()
            .Bind(configuration.GetSection(CorsPolicyOptions.Section))
            .Validate(options => options.AllowedOrigins.All(IsAbsoluteHttpOrigin),
                $"{CorsPolicyOptions.Section}:AllowedOrigins must contain absolute http(s) origins only.")
            .ValidateOnStart();

        // Credentials are required for SignalR when the client is on another origin.
        services.AddCors();
        services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>()
            .Configure<IOptions<CorsPolicyOptions>>((cors, origins) => cors.AddPolicy(CorsPolicyOptions.PolicyName, policy =>
                policy.WithOrigins(origins.Value.AllowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .WithExposedHeaders(HeaderNames.ContentDisposition)));

        return services;
    }

    private static bool IsAbsoluteHttpOrigin(string origin) =>
        Uri.TryCreate(origin, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");
        api.MapAuthEndpoints();
        api.MapAdminEndpoints();
        api.MapExpenseEndpoints();
        api.MapCategoryEndpoints();
        api.MapBudgetEndpoints();

        // Signed-in users only. The connection is closed when its access token expires, and the client
        // reconnects with a fresh one, so a revoked session can't keep listening.
        app.MapHub<ExpenseHub>(ExpenseHub.Path, options => options.CloseOnAuthenticationExpiration = true)
            .RequireAuthorization();
        return app;
    }

    public static void LogStartupConfiguration(this WebApplication app, FeatureOptions features)
    {
        var origins = app.Services.GetRequiredService<IOptions<CorsPolicyOptions>>().Value.AllowedOrigins;
        LogStartup(app.Logger, app.Environment.EnvironmentName, features.IsApiDocsEnabled, features.ShouldMigrateDatabaseOnStartup,
            string.Join(", ", origins), !string.IsNullOrWhiteSpace(app.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]));
    }

    [LoggerMessage(EventId = LogEvents.StartupConfiguration, Level = LogLevel.Information,
        Message = "Starting in {Environment}: API docs {ApiDocs}, migrate on startup {MigrateOnStartup}, CORS origins [{CorsOrigins}], OTLP export {OtlpExport}")]
    private static partial void LogStartup(
        ILogger logger, string environment, bool apiDocs, bool migrateOnStartup, string corsOrigins, bool otlpExport);
}
