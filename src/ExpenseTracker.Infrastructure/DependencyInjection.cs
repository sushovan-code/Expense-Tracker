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

namespace ExpenseTracker.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    private static readonly TimeSpan MinRefreshTokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan MaxRefreshTokenLifetime = TimeSpan.FromDays(90);

    /// <summary>Registers SQLite persistence and the read-side queries behind the Application ports.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Reads <c>ConnectionStrings:DefaultConnection</c> and the <c>Identity</c> section.</param>
    /// <param name="environmentName">Used only to make a missing-configuration error actionable.</param>
    /// <param name="contentRootPath">A relative SQLite <c>Data Source</c> is resolved against this folder.</param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string environmentName, string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"No database connection string for the '{environmentName}' environment. Set " +
                $"ConnectionStrings:{ConnectionStringName} in configs/appsettings.{environmentName}.json " +
                $"or the ConnectionStrings__{ConnectionStringName} environment variable.");
        }

        var sqliteConnectionString = SqliteConnectionStrings.ResolveDataSource(connectionString, contentRootPath);
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(sqliteConnectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IExpenseQueries, ExpenseQueries>();
        services.AddScoped<ICategoryQueries, CategoryQueries>();
        services.AddScoped<IBudgetQueries, BudgetQueries>();
        services.AddScoped<DatabaseInitializer>();

        services.AddIdentityStore(configuration);

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        return services;
    }

    /// <summary>
    /// ASP.NET Core Identity's user/role store (core + EF stores only: no cookies or UI, the API issues
    /// its own tokens), plus sessions, API keys, the cached access snapshot and the seeder.
    /// </summary>
    private static void AddIdentityStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<IdentityStoreOptions>()
            .Bind(configuration.GetSection(IdentityStoreOptions.Section))
            .Validate(options => options.RefreshTokenLifetime >= MinRefreshTokenLifetime && options.RefreshTokenLifetime <= MaxRefreshTokenLifetime,
                $"{IdentityStoreOptions.Section}:RefreshTokenLifetime must be between {MinRefreshTokenLifetime} and {MaxRefreshTokenLifetime}.")
            .ValidateOnStart();

        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = IdentityRules.MinPasswordLength;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = IdentityRules.MaxFailedSignInAttempts;
                options.Lockout.DefaultLockoutTimeSpan = IdentityRules.LockoutDuration;
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
