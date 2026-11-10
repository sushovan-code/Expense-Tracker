using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Infrastructure.Persistence;

namespace ExpenseTracker.Api.Auth;

/// <summary>
/// Runs after authentication and authorisation. Looks up the signed-in user's family once per request so the
/// query filters and use cases know whose data to show. It only reads; it never changes the principal or the
/// app roles, so the existing RBAC and JWT flow are untouched.
/// </summary>
internal sealed class FamilyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, FamilyContext familyContext, IFamilyQueries queries)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.GetUserId();
            var membership = await queries.FindMembershipAsync(userId, context.RequestAborted);
            familyContext.Set(userId, membership?.FamilyId, membership?.Role);
        }

        await next(context);
    }
}
