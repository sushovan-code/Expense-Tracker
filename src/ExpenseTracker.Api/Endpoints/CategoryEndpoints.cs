using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Http;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

internal static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/categories").WithTags("Categories")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", List)
            .RequirePermission(Permissions.Categories.View)
            .WithName("ListCategories")
            .WithSummary("List categories (paged)")
            .WithDescription("Server-side paging, sorting and name search, with each category's expense count and budget.")
            .ProducesValidationProblem();

        group.MapGet("/lookup", Lookup)
            .RequirePermission(Permissions.Categories.View)
            .WithName("LookupCategories")
            .WithSummary("All categories (id, name, color)")
            .WithDescription("Unpaged, minimal list for dropdowns and filters.");

        group.MapPost("/", Create)
            .RequirePermission(Permissions.Categories.Add)
            .WithName("CreateCategory")
            .WithSummary("Create a category")
            .ProducesValidationProblem();

        group.MapPut("/{id:guid}", Update)
            .RequirePermission(Permissions.Categories.Edit)
            .WithName("UpdateCategory")
            .WithSummary("Update a category")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", Delete)
            .RequirePermission(Permissions.Categories.Delete)
            .WithName("DeleteCategory")
            .WithSummary("Delete a category")
            .WithDescription("Fails with 409 Conflict while the category still has expenses.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return routes;
    }

    private static async Task<Results<Ok<PagedResult<CategoryDto>>, ProblemHttpResult>> List(
        [AsParameters] CategoryListQuery query, ICategoryService service, CancellationToken cancellationToken) =>
        (await service.ListAsync(query.ToRequest(), cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<Ok<IReadOnlyList<CategoryLookupDto>>> Lookup(ICategoryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.LookupAsync(cancellationToken));

    private static async Task<Results<Created<CategoryDto>, ProblemHttpResult>> Create(
        CategoryRequest request, ICategoryService service, CancellationToken cancellationToken) =>
        (await service.CreateAsync(request.ToCommand(), cancellationToken))
            .ToHttpResult(c => TypedResults.Created($"{ApiRoutes.Prefix}/categories/{c.Id}", c));

    private static async Task<Results<Ok<CategoryDto>, ProblemHttpResult>> Update(
        Guid id, CategoryRequest request, ICategoryService service, CancellationToken cancellationToken) =>
        (await service.UpdateAsync(id, request.ToCommand(), cancellationToken)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> Delete(
        Guid id, ICategoryService service, CancellationToken cancellationToken) =>
        (await service.DeleteAsync(id, cancellationToken)).ToHttpResult();
}
