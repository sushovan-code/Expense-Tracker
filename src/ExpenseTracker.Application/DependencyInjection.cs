using ExpenseTracker.Application.Features.Budgets;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Features.Expenses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExpenseTracker.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use cases. The host must also register implementations of the ports in
    /// <c>Abstractions</c> (persistence, queries, realtime) — e.g. via AddInfrastructure().
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<BudgetService>();
        services.AddScoped<IBudgetService>(sp => sp.GetRequiredService<BudgetService>());
        services.AddScoped<IBudgetMonitor>(sp => sp.GetRequiredService<BudgetService>());

        return services;
    }
}
