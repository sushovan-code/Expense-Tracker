using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Auth;

internal static class PermissionEndpointExtensions
{
    /// <summary>Requires an authenticated caller holding every one of <paramref name="permissions"/>.</summary>
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, params string[] permissions)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(new RequirePermissionAttribute(permissions));
}
