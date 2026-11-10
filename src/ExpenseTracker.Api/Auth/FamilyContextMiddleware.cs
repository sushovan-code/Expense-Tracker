using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Infrastructure.Persistence;

namespace ExpenseTracker.Api.Auth;

/// <summary>
/// Runs after authentication and authorisation. Looks up the signed-in user's families once per request and
/// picks the active one, so the query filters and use cases know whose data to show. It only reads; it never
/// changes the principal or the app roles, so the existing RBAC and JWT flow are untouched.
/// </summary>
/// <remarks>
/// The browser names the family it is working in with the <c>X-Family-Id</c> header (or the <c>familyId</c> query
/// parameter on the SignalR connection). A family the person doesn't belong to is ignored, so the header can never
/// widen access: it only chooses among the caller's own families, defaulting to the earliest joined.
/// </remarks>
internal sealed class FamilyContextMiddleware(RequestDelegate next)
{
    public const string FamilyHeader = "X-Family-Id";
    private const string FamilyQuery = "familyId";

    public async Task InvokeAsync(HttpContext context, FamilyContext familyContext, IFamilyQueries queries)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.GetUserId();
            var families = await queries.ListForUserAsync(userId, context.RequestAborted);

            var requested = Guid.TryParse(context.Request.Headers[FamilyHeader].FirstOrDefault() ?? context.Request.Query[FamilyQuery].FirstOrDefault(), out var id) ? id : (Guid?)null;
            var active = families.FirstOrDefault(f => f.Id == requested) ?? families.FirstOrDefault();
            familyContext.Set(userId, active?.Id, active?.Role);
        }

        await next(context);
    }
}
