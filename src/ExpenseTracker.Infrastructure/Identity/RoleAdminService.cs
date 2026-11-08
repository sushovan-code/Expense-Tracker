using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Role permissions are stored as Identity role claims of type <c>permission</c>.</summary>
internal sealed partial class RoleAdminService(
    AppDbContext dbContext,
    IUserAccessProvider access,
    ILogger<RoleAdminService> logger) : IRoleAdminService
{
    private static readonly IReadOnlyList<string> AllPermissions = Permissions.All.Select(p => p.Name).ToArray();

    public async Task<IReadOnlyList<RoleDto>> ListAsync(Actor actor, CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.AsNoTracking().Select(r => new { r.Id, r.Name }).ToListAsync(cancellationToken);
        var claims = await dbContext.RoleClaims.AsNoTracking()
            .Where(c => c.ClaimType == AppClaimTypes.Permission)
            .Select(c => new { c.RoleId, c.ClaimValue })
            .ToListAsync(cancellationToken);
        var counts = await dbContext.UserRoles.GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        var result = new List<RoleDto>();
        foreach (var definition in AppRoles.All)
        {
            var role = roles.FirstOrDefault(r => string.Equals(r.Name, definition.Name, StringComparison.OrdinalIgnoreCase));
            if (role is null) continue;

            var permissions = definition.Name == AppRoles.SuperAdmin
                ? AllPermissions
                : claims.Where(c => c.RoleId == role.Id && Permissions.IsKnown(c.ClaimValue!))
                    .Select(c => c.ClaimValue!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            result.Add(new RoleDto(definition.Name, definition.Rank, definition.Description, permissions,
                counts.GetValueOrDefault(role.Id), AccessPolicy.CanEditRole(actor, definition.Name) is null));
        }
        return result;
    }

    public async Task<Result<RoleDto>> SetPermissionsAsync(
        Actor actor, string role, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        if (AccessPolicy.CanEditRole(actor, role) is { } denied)
        {
            if (denied.Kind == ErrorKind.Forbidden) LogDenied(logger, actor.UserId, role, denied.Detail);
            return denied;
        }

        var requested = permissions.Select(p => p.Trim()).Distinct(StringComparer.Ordinal).ToList();
        if (requested.FirstOrDefault(p => !Permissions.IsKnown(p)) is { } unknown)
            return Error.Validation("permissions", $"Unknown permission '{unknown}'.");

        var normalizedName = role.ToUpperInvariant();
        var entity = await dbContext.Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
        if (entity is null) return new Error(ErrorKind.NotFound, "Role not found", $"Role '{role}' does not exist.");

        var current = await dbContext.RoleClaims
            .Where(c => c.RoleId == entity.Id && c.ClaimType == AppClaimTypes.Permission)
            .ToListAsync(cancellationToken);
        var currentValues = current.Select(c => c.ClaimValue!).ToHashSet(StringComparer.Ordinal);

        var toAdd = requested.Where(p => !currentValues.Contains(p)).ToList();
        var toRemove = current.Where(c => !requested.Contains(c.ClaimValue!, StringComparer.Ordinal)).ToList();

        // You can only add what you hold yourself; taking permissions away is always allowed.
        if (AccessPolicy.CanGrantPermissions(actor, toAdd) is { } cannotGrant)
        {
            if (cannotGrant.Kind == ErrorKind.Forbidden) LogDenied(logger, actor.UserId, role, cannotGrant.Detail);
            return cannotGrant;
        }

        dbContext.RoleClaims.RemoveRange(toRemove);
        dbContext.RoleClaims.AddRange(toAdd.Select(p => new IdentityRoleClaim<Guid>
        {
            RoleId = entity.Id,
            ClaimType = AppClaimTypes.Permission,
            ClaimValue = p,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);

        // Every member of the role is affected.
        await access.InvalidateAllAsync(cancellationToken);
        LogRolePermissionsChanged(logger, actor.UserId, entity.Name!,
            string.Join(", ", toAdd), string.Join(", ", toRemove.Select(c => c.ClaimValue)));

        return (await ListAsync(actor, cancellationToken)).Single(r => r.Name == entity.Name);
    }

    [LoggerMessage(EventId = LogEvents.RolePermissionsChanged, Level = LogLevel.Information,
        Message = "User {ActorId} changed permissions of role {Role}: added [{Added}], removed [{Removed}]")]
    private static partial void LogRolePermissionsChanged(ILogger logger, Guid actorId, string role, string added, string removed);

    [LoggerMessage(EventId = LogEvents.AdministrationDenied, Level = LogLevel.Warning,
        Message = "Denied change to role {Role} by user {ActorId}: {Reason}")]
    private static partial void LogDenied(ILogger logger, Guid actorId, string role, string reason);
}
