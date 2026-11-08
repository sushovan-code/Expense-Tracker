using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Auth;

/// <summary>Requires every listed permission claim.</summary>
internal sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;

    public override string ToString() => $"PermissionRequirement: {string.Join(", ", Permissions)}";
}

/// <summary>
/// Endpoint metadata carrying a <see cref="PermissionRequirement"/>. Implementing
/// <see cref="IAuthorizationRequirementData"/> lets the authorization middleware build the policy from the
/// endpoint itself: no named policy per permission and no custom policy provider.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RequirePermissionAttribute(params string[] permissions) : AuthorizeAttribute, IAuthorizationRequirementData
{
    public IReadOnlyList<string> Permissions { get; } = permissions;

    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new PermissionRequirement(Permissions)];
}

internal sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (requirement.Permissions.All(p => context.User.HasClaim(AppClaimTypes.Permission, p)))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>Requires a role ranked at or above <see cref="Role"/> ("Admin or above").</summary>
internal sealed class MinimumRoleRequirement(string role) : IAuthorizationRequirement
{
    public string Role { get; } = role;
    public int Rank { get; } = AppRoles.RankOf(role);

    public override string ToString() => $"MinimumRoleRequirement: {Role} or above";
}

internal sealed class MinimumRoleAuthorizationHandler : AuthorizationHandler<MinimumRoleRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MinimumRoleRequirement requirement)
    {
        var rank = AppRoles.HighestRank(context.User.FindAll(AppClaimTypes.Role).Select(c => c.Value));
        if (rank >= requirement.Rank) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

internal static class PermissionEndpointExtensions
{
    /// <summary>Requires an authenticated caller holding every one of <paramref name="permissions"/>.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(new RequirePermissionAttribute(permissions));
}
