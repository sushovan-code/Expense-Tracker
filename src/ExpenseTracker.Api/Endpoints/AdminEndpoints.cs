using System.Security.Claims;
using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Http;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

/// <summary>
/// User, role and claim administration. Two gates: the caller's role must be Admin or above, and each
/// operation needs its specific permission. On top of that the use cases enforce the role hierarchy
/// (no managing yourself or anyone at or above your rank, no granting what you don't hold).
/// </summary>
internal static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder routes)
    {
        var admin = routes.MapGroup("/admin")
            .RequireAuthorization(AuthPolicies.AdminOrAbove)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        MapUsers(admin.MapGroup("/users").WithTags("Admin: users"));
        MapRoles(admin.MapGroup("/roles").WithTags("Admin: roles"));

        admin.MapGet("/permissions", ListPermissions)
            .WithTags("Admin: roles")
            .RequirePermission(Permissions.Roles.View)
            .WithName("ListPermissions")
            .WithSummary("The permission catalog");

        return routes;
    }

    private static void MapUsers(RouteGroupBuilder users)
    {
        users.MapGet("/", ListUsers)
            .RequirePermission(Permissions.Users.View)
            .WithName("ListUsers")
            .WithSummary("List users (paged)")
            .ProducesValidationProblem();

        users.MapGet("/{id:guid}", GetUser)
            .RequirePermission(Permissions.Users.View)
            .WithName("GetUser")
            .WithSummary("Get a user, with roles, claims and effective permissions")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/", CreateUser)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("CreateUser")
            .WithSummary("Create a user")
            .WithDescription("You can only assign roles below your own (SuperAdmins can assign any role).")
            .ProducesValidationProblem();

        users.MapPut("/{id:guid}", UpdateUser)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("UpdateUser")
            .WithSummary("Update a user's email, name and active state")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapDelete("/{id:guid}", DeleteUser)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("DeleteUser")
            .WithSummary("Delete a user")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPut("/{id:guid}/roles", SetRoles)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("SetUserRoles")
            .WithSummary("Replace a user's roles")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/{id:guid}/claims", AddClaim)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("AddUserClaim")
            .WithSummary("Add a claim to a user")
            .WithDescription("A 'permission' claim grants that permission directly, on top of the user's roles.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapDelete("/{id:guid}/claims", RemoveClaim)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("RemoveUserClaim")
            .WithSummary("Remove a claim from a user")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/{id:guid}/reset-password", ResetPassword)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("ResetUserPassword")
            .WithSummary("Set a new password for a user")
            .WithDescription("Also unlocks the account and signs the user out everywhere.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/{id:guid}/unlock", Unlock)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("UnlockUser")
            .WithSummary("Clear a lockout after too many failed sign-ins")
            .ProducesProblem(StatusCodes.Status404NotFound);

        users.MapPost("/{id:guid}/revoke-sessions", RevokeSessions)
            .RequirePermission(Permissions.Users.Manage)
            .WithName("RevokeUserSessions")
            .WithSummary("Sign a user out everywhere")
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static void MapRoles(RouteGroupBuilder roles)
    {
        roles.MapGet("/", ListRoles)
            .RequirePermission(Permissions.Roles.View)
            .WithName("ListRoles")
            .WithSummary("Roles with their permissions and member counts");

        roles.MapPut("/{name}/permissions", SetRolePermissions)
            .RequirePermission(Permissions.Roles.Manage)
            .WithName("SetRolePermissions")
            .WithSummary("Replace a role's permissions")
            .WithDescription("Only roles below your own; SuperAdmin is fixed. You can only add permissions you hold.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    // ---- Users ----

    private static async Task<Results<Ok<PagedResult<UserSummaryDto>>, ProblemHttpResult>> ListUsers(
        [AsParameters] UserListQuery query, IUserAdminService service, CancellationToken ct) =>
        (await service.ListAsync(query.ToRequest(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<UserDetailDto>, ProblemHttpResult>> GetUser(
        Guid id, IUserAdminService service, CancellationToken ct) =>
        (await service.GetAsync(id, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<CreatedAtRoute<UserDetailDto>, ProblemHttpResult>> CreateUser(
        CreateUserRequest request, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.CreateAsync(user.ToActor(), request.ToCommand(), ct))
            .ToHttpResult(created => TypedResults.CreatedAtRoute(created, "GetUser", new { id = created.Id }));

    private static async Task<Results<Ok<UserDetailDto>, ProblemHttpResult>> UpdateUser(
        Guid id, UpdateUserRequest request, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.UpdateAsync(user.ToActor(), id, request.ToCommand(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> DeleteUser(
        Guid id, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.DeleteAsync(user.ToActor(), id, ct)).ToHttpResult();

    private static async Task<Results<Ok<UserDetailDto>, ProblemHttpResult>> SetRoles(
        Guid id, SetUserRolesRequest request, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.SetRolesAsync(user.ToActor(), id, request.Roles, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<UserDetailDto>, ProblemHttpResult>> AddClaim(
        Guid id, UserClaimRequest request, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.AddClaimAsync(user.ToActor(), id, request.ToClaim(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<UserDetailDto>, ProblemHttpResult>> RemoveClaim(
        Guid id, [AsParameters] UserClaimRequest request, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.RemoveClaimAsync(user.ToActor(), id, request.ToClaim(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> ResetPassword(
        Guid id, ResetPasswordRequest request, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.ResetPasswordAsync(user.ToActor(), id, request.NewPassword, ct)).ToHttpResult();

    private static async Task<Results<NoContent, ProblemHttpResult>> Unlock(
        Guid id, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.UnlockAsync(user.ToActor(), id, ct)).ToHttpResult();

    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeSessions(
        Guid id, ClaimsPrincipal user, IUserAdminService service, CancellationToken ct) =>
        (await service.RevokeSessionsAsync(user.ToActor(), id, ct)).ToHttpResult();

    // ---- Roles ----

    private static async Task<Ok<IReadOnlyList<RoleDto>>> ListRoles(
        ClaimsPrincipal user, IRoleAdminService service, CancellationToken ct) =>
        TypedResults.Ok(await service.ListAsync(user.ToActor(), ct));

    private static async Task<Results<Ok<RoleDto>, ProblemHttpResult>> SetRolePermissions(
        string name, SetRolePermissionsRequest request, ClaimsPrincipal user, IRoleAdminService service, CancellationToken ct) =>
        (await service.SetPermissionsAsync(user.ToActor(), name, request.Permissions, ct)).ToHttpResult(TypedResults.Ok);

    private static Ok<IReadOnlyList<PermissionInfo>> ListPermissions() => TypedResults.Ok(Permissions.All);
}
