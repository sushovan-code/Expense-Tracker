using ExpenseTracker.Api.Auth;
using ExpenseTracker.Api.Contracts;
using ExpenseTracker.Api.Http;
using ExpenseTracker.Application.Features.Families;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ExpenseTracker.Api.Endpoints;

/// <summary>
/// Family management. Needs a signed-in user (the fallback policy); what each person may do is decided by
/// their family role in the use case, not by the app-wide permissions.
/// </summary>
internal static class FamilyEndpoints
{
    public static IEndpointRouteBuilder MapFamilyEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/families").WithTags("Families")
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", ListMine).WithName("ListMyFamilies").WithSummary("The families I belong to");
        group.MapGet("/mine", GetMine).WithName("GetMyFamily").WithSummary("Get my active family, its members and open invites")
            .WithDescription("The active family is the one named by the X-Family-Id header, else the earliest joined.")
            .ProducesProblem(StatusCodes.Status403Forbidden);
        group.MapPost("/", Create).WithName("CreateFamily").WithSummary("Create a family (I become its owner); I can create several")
            .ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/mine", Rename).WithName("RenameFamily").WithSummary("Rename my family (owner)")
            .ProducesValidationProblem();

        group.MapPost("/members", AddMember).WithName("AddFamilyMember").WithSummary("Create an account and add it to my family (owner)")
            .ProducesValidationProblem().ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/members/{userId:guid}/role", ChangeRole).WithName("ChangeFamilyRole").WithSummary("Change a member's role (owner)")
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/members/{userId:guid}", RemoveMember).WithName("RemoveFamilyMember").WithSummary("Remove a member (owner)")
            .ProducesProblem(StatusCodes.Status404NotFound).ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/leave", Leave).WithName("LeaveFamily").WithSummary("Leave my family")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/invites", CreateInvite).WithName("CreateFamilyInvite")
            .WithSummary("Create an invite link token (owner)")
            .WithDescription("The token is returned once and is valid for 48 hours and one use. Share it as a link yourself.");
        group.MapDelete("/invites/{inviteId:guid}", RevokeInvite).WithName("RevokeFamilyInvite").WithSummary("Revoke an invite (owner)")
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/invites/join", Join).WithName("JoinFamily")
            .WithSummary("Join a family with an invite token (signed-in user without a family)")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/invites/accept", Accept)
            .AllowAnonymous()
            .RequireRateLimiting(AuthSetup.AuthRateLimitPolicy)
            .WithName("AcceptFamilyInvite")
            .WithSummary("Create an account from an invite and join the family")
            .WithDescription("Anonymous and rate-limited. Afterwards the person signs in normally.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return routes;
    }

    private static async Task<Ok<IReadOnlyList<FamilySummaryDto>>> ListMine(IFamilyService service, CancellationToken ct) =>
        TypedResults.Ok(await service.ListMineAsync(ct));

    private static async Task<Results<Ok<FamilyDto>, ProblemHttpResult>> GetMine(IFamilyService service, CancellationToken ct) =>
        (await service.GetMineAsync(ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<FamilyDto>, ProblemHttpResult>> Create(
        CreateFamilyRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.CreateAsync(request.Name, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<FamilyDto>, ProblemHttpResult>> Rename(
        CreateFamilyRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.RenameAsync(request.Name, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<Ok<FamilyDto>, ProblemHttpResult>> AddMember(
        AddFamilyMemberRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.AddMemberAsync(request.ToCommand(), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> ChangeRole(
        Guid userId, ChangeFamilyRoleRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.ChangeRoleAsync(userId, request.Role, ct)).ToHttpResult();

    private static async Task<Results<NoContent, ProblemHttpResult>> RemoveMember(
        Guid userId, IFamilyService service, CancellationToken ct) =>
        (await service.RemoveMemberAsync(userId, ct)).ToHttpResult();

    private static async Task<Results<NoContent, ProblemHttpResult>> Leave(IFamilyService service, CancellationToken ct) =>
        (await service.LeaveAsync(ct)).ToHttpResult();

    private static async Task<Results<Ok<CreatedInviteDto>, ProblemHttpResult>> CreateInvite(
        CreateInviteRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.CreateInviteAsync(request.Role, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> RevokeInvite(
        Guid inviteId, IFamilyService service, CancellationToken ct) =>
        (await service.RevokeInviteAsync(inviteId, ct)).ToHttpResult();

    private static async Task<Results<Ok<FamilyDto>, ProblemHttpResult>> Join(
        JoinFamilyRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.JoinAsync(request.Token, ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<Results<NoContent, ProblemHttpResult>> Accept(
        AcceptInviteRequest request, IFamilyService service, CancellationToken ct) =>
        (await service.AcceptInviteAsync(request.ToCommand(), ct)).ToHttpResult();
}
