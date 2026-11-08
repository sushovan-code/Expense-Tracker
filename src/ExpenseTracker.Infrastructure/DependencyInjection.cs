using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Infrastructure.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using ExpenseTracker.Infrastructure.Persistence.Queries;
using ExpenseTracker.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace ExpenseTracker.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    /// <summary>Registers SQL Server persistence and the read-side queries behind the Application ports.</summary>
    /// <param name="environmentName">Used only to make a missing-configuration error actionable.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string environmentName)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No database connection string for the '{environmentName}' environment. Set " +
                $"ConnectionStrings:{ConnectionStringName} in configs/appsettings.{environmentName}.json " +
                $"or the ConnectionStrings__{ConnectionStringName} environment variable.");
        }

        // Retries transient faults (e.g. LocalDB/Azure SQL waking up) instead of failing the first request.
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IExpenseQueries, ExpenseQueries>();
        services.AddScoped<ICategoryQueries, CategoryQueries>();
        services.AddScoped<IBudgetQueries, BudgetQueries>();
        services.AddScoped<DatabaseInitializer>();

        services.AddIdentityStore(configuration);

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        // SQL commands appear as child spans of the request in traces.
        services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddSqlClientInstrumentation());

        return services;
    }

    /// <summary>
    /// ASP.NET Core Identity's user/role store (core + EF stores only: no cookies or UI, the API issues
    /// its own tokens), plus sessions, API keys, the cached access snapshot and the seeder.
    /// </summary>
    private static void AddIdentityStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentitySettings>().Bind(configuration.GetSection(IdentitySettings.Section));

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                // Length over composition (NIST SP 800-63B), while keeping mixed case and a digit.
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddHybridCache();

        services.AddScoped<IUserAccessProvider, UserAccessProvider>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();
        services.AddScoped<IdentitySeeder>();
    }
}
