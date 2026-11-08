using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Auth;

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
