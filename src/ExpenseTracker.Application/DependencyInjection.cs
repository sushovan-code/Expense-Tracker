using ExpenseTracker.Application.Features.Budgets;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Application.Features.Families;
using ExpenseTracker.Application.Features.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseTracker.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases. The host must also register implementations of the ports in
    /// <c>Abstractions</c> (persistence, queries, realtime, identity) and a <see cref="Security.SessionPolicy"/>,
    /// e.g. via AddInfrastructure().
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IFamilyService, FamilyService>();

        services.AddScoped<BudgetService>();
        services.AddScoped<IBudgetService>(sp => sp.GetRequiredService<BudgetService>());
        services.AddScoped<IBudgetMonitor>(sp => sp.GetRequiredService<BudgetService>());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IRoleAdminService, RoleAdminService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        return services;
    }
}
