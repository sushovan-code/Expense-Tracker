using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Identity;

/// <summary>Role → permission administration. SuperAdmin always holds every permission, whatever is stored.</summary>
internal sealed partial class RoleAdminService(
    IRolePermissionStore roles,
    IUserAccessProvider access,
    ILogger<RoleAdminService> logger) : IRoleAdminService
{
    private static readonly IReadOnlyList<string> AllPermissions = Permissions.All.Select(p => p.Name).ToArray();

    public async Task<IReadOnlyList<RoleDto>> ListAsync(Actor actor, CancellationToken cancellationToken)
    {
        var stored = await roles.ListAsync(cancellationToken);

        var result = new List<RoleDto>();
        foreach (var definition in AppRoles.All)
        {
            var role = stored.FirstOrDefault(r => string.Equals(r.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            if (role is null) continue;

            var permissions = definition.Name == AppRoles.SuperAdmin
                ? AllPermissions
                : role.Permissions.Where(Permissions.IsKnown).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            result.Add(new RoleDto(definition.Name, definition.Rank, definition.Description, permissions,
                role.UserCount, AccessPolicy.CanEditRole(actor, definition.Name) is null));
        }
        return result;
    }

    public async Task<Result<RoleDto>> SetPermissionsAsync(
        Actor actor, string role, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        if (Deny(actor, role, AccessPolicy.CanEditRole(actor, role)) is { } denied) return denied;

        var requested = permissions.Select(p => p.Trim()).Distinct(StringComparer.Ordinal).ToList();
        if (requested.FirstOrDefault(p => !Permissions.IsKnown(p)) is { } unknown)
            return Error.Validation("permissions", $"Unknown permission '{unknown}'.");

        var stored = await roles.FindAsync(role, cancellationToken);
        if (stored is null) return new Error(ErrorKind.NotFound, "Role not found", $"Role '{role}' does not exist.");

        var toAdd = requested.Except(stored.Permissions, StringComparer.Ordinal).ToList();
        var toRemove = stored.Permissions.Where(p => !requested.Contains(p, StringComparer.Ordinal)).ToList();

        // You can only add what you hold yourself; taking permissions away is always allowed.
        if (Deny(actor, role, AccessPolicy.CanGrantPermissions(actor, toAdd)) is { } cannotGrant) return cannotGrant;

        await roles.UpdateAsync(stored.Name, toAdd, toRemove, cancellationToken);

        // Every member of the role is affected.
        await access.InvalidateAllAsync(cancellationToken);
        LogRolePermissionsChanged(logger, actor.UserId, stored.Name, string.Join(", ", toAdd), string.Join(", ", toRemove));

        return (await ListAsync(actor, cancellationToken)).Single(r => r.Name == stored.Name);
    }

    private Error? Deny(Actor actor, string role, Error? error)
    {
        if (error?.Kind == ErrorKind.Forbidden) LogDenied(logger, actor.UserId, role, error.Detail);
        return error;
    }

    [LoggerMessage(EventId = LogEvents.RolePermissionsChanged, Level = LogLevel.Information,
        Message = "User {ActorId} changed permissions of role {Role}: added [{Added}], removed [{Removed}]")]
    private static partial void LogRolePermissionsChanged(ILogger logger, Guid actorId, string role, string added, string removed);

    [LoggerMessage(EventId = LogEvents.AdministrationDenied, Level = LogLevel.Warning,
        Message = "Denied change to role {Role} by user {ActorId}: {Reason}")]
    private static partial void LogDenied(ILogger logger, Guid actorId, string role, string reason);
}
