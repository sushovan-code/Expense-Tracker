using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Auth;

internal sealed class MinimumRoleAuthorizationHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement)
    {
        var rank = AppRoles.HighestRank(context.User.FindAll(AppClaimTypes.Role).Select(c => c.Value));
        if (rank >= requirement.Rank) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
