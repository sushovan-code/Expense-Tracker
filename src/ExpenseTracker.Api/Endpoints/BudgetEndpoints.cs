using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Application.Features.Budgets;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

internal static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGroup("/budgets").WithTags("Budgets")
            .MapGet("/", Overview)
            .RequirePermission(Permissions.Budgets.View)
            .WithName("GetBudgetOverview")
            .WithSummary("Budget status for a month")
            .WithDescription("Every category that has a monthly budget, with how much of it has been spent.")
            .ProducesValidationProblem();

        return routes;
    }

    private static async Task<Ok<BudgetOverviewDto>> Overview(
        [AsParameters] BudgetQuery query, IBudgetService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetOverviewAsync(query.Year, query.Month, ct));
}
