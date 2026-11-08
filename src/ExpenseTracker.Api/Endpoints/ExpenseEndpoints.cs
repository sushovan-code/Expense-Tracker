using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Http;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

internal static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/expenses").WithTags("Expenses")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", List)
            .RequirePermission(Permissions.Expenses.View)
            .WithName("ListExpenses")
            .WithSummary("List expenses (paged)")
            .WithDescription("Server-side paging, sorting and filtering. Includes the total count and total amount of the filtered set.")
            .ProducesValidationProblem();

        group.MapGet("/export", Export)
            .RequirePermission(Permissions.Expenses.Export)
            .WithName("ExportExpenses")
            .WithSummary("Export expenses as CSV")
            .WithDescription($"Same filters and sort as the list, without paging (up to {ExpenseExport.MaxRows:N0} rows).")
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .ProducesValidationProblem();

        group.MapGet("/summary", Summary)
            .RequirePermission(Permissions.Expenses.View)
            .WithName("GetExpenseSummary")
            .WithSummary("Spending summary")
            .WithDescription("Totals for a date range, broken down by category and by month.")
            .ProducesValidationProblem();

        group.MapGet("/{id:int}", Get)
            .RequirePermission(Permissions.Expenses.View)
            .WithName("GetExpense")
            .WithSummary("Get an expense")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .RequirePermission(Permissions.Expenses.Add)
            .WithName("CreateExpense")
            .WithSummary("Create an expense")
            .ProducesValidationProblem();

        group.MapPut("/{id:int}", Update)
            .RequirePermission(Permissions.Expenses.Edit)
            .WithName("UpdateExpense")
            .WithSummary("Update an expense")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:int}", Delete)
            .RequirePermission(Permissions.Expenses.Delete)
            .WithName("DeleteExpense")
            .WithSummary("Delete an expense")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/bulk-delete", BulkDelete)
            .RequirePermission(Permissions.Expenses.Delete)
            .WithName("BulkDeleteExpenses")
            .WithSummary("Delete several expenses")
            .WithDescription("Deletes the given ids; ids that no longer exist are ignored and reported via the count.")
            .ProducesValidationProblem();

        return routes;
    }

    private static async Task<Results<Ok<ExpensePage>, ProblemHttpResult>> List(
        [AsParameters] ExpenseListQuery query, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.ListAsync(query.ToRequest(), cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> Export(
        [AsParameters] ExpenseExportQuery query, IExpenseService service, TimeProvider clock, CancellationToken cancellationToken) =>
        (await service.ExportAsync(query.ToFilter(), query.SortBy, query.SortDir, cancellationToken)).ToHttpResult(rows =>
            TypedResults.File(ExpenseCsvWriter.Write(rows), "text/csv; charset=utf-8", $"expenses-{clock.GetLocalNow():yyyyMMdd-HHmm}.csv"));

    private static async Task<Results<Ok<ExpenseSummary>, ProblemHttpResult>> Summary(
        [AsParameters] SummaryQuery query, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.SummarizeAsync(query.From, query.To, cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<ExpenseDto>, ProblemHttpResult>> Get(
        int id, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.GetAsync(id, cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<CreatedAtRoute<ExpenseDto>, ProblemHttpResult>> Create(
        ExpenseRequest request, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.CreateAsync(request.ToCommand(), cancellationToken))
            .ToHttpResult(e => TypedResults.CreatedAtRoute(e, "GetExpense", new { id = e.Id }));

    private static async Task<Results<Ok<ExpenseDto>, ProblemHttpResult>> Update(
        int id, ExpenseRequest request, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.UpdateAsync(id, request.ToCommand(), cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        int id, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.DeleteAsync(id, cancellationToken)).ToHttpResult();

    private static async Task<Results<Ok<BulkDeleteResult>, ProblemHttpResult>> BulkDelete(
        BulkDeleteRequest request, IExpenseService service, CancellationToken cancellationToken) =>
        (await service.BulkDeleteAsync(request.Ids, cancellationToken)).ToHttpResult(TypedResults.Ok);
}
