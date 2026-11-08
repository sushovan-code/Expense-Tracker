using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Security;

/// <summary>The authenticated user performing an administrative action.</summary>
public sealed record Actor(Guid UserId, IReadOnlyCollection<string> Roles, IReadOnlySet<string> Permissions)
{
    public int Rank => AppRoles.HighestRank(Roles);

    public bool IsSuperAdmin => Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

    public bool Has(string permission) => Permissions.Contains(permission);
}

/// <summary>
/// Anti-privilege-escalation rules for user, role and API key administration. Pure functions, so the
/// rules are testable in isolation; the identity adapters call them before changing anything.
/// Each returns <c>null</c> when allowed, or the error to return.
/// </summary>
public static class AccessPolicy
{
    /// <summary>
    /// You manage users strictly below your rank (SuperAdmins manage everyone), and never yourself:
    /// self-service changes go through the account endpoints, so nobody can lift their own role or lock themselves out.
    /// </summary>
    public static Error? CanManageUser(Actor actor, Guid targetUserId, IEnumerable<string> targetRoles)
    {
        if (actor.UserId == targetUserId)
            return Error.Forbidden("You can't change your own account from user administration. Use your account page instead.");

        if (!actor.IsSuperAdmin && AppRoles.HighestRank(targetRoles) >= actor.Rank)
            return Error.Forbidden("You can only manage users whose role is below your own.");

        return null;
    }

    /// <summary>You can only hand out roles below your own (SuperAdmins can hand out any role).</summary>
    public static Error? CanAssignRoles(Actor actor, IEnumerable<string> roles)
    {
        foreach (var role in roles)
        {
            if (!AppRoles.IsKnown(role))
                return Error.Validation("roles", $"Unknown role '{role}'.");

            if (!actor.IsSuperAdmin && AppRoles.RankOf(role) >= actor.Rank)
                return Error.Forbidden($"You can't assign the {role} role: you can only assign roles below your own.");
        }

        return null;
    }

    /// <summary>You can only grant permissions you hold yourself, so a grant can never exceed the granter.</summary>
    public static Error? CanGrantPermissions(Actor actor, IEnumerable<string> permissions, string field = "permissions")
    {
        foreach (var permission in permissions)
        {
            if (!Permissions.IsKnown(permission))
                return Error.Validation(field, $"Unknown permission '{permission}'.");

            if (!actor.Has(permission))
                return Error.Forbidden($"You can't grant '{permission}' because you don't hold it yourself.");
        }

        return null;
    }

    /// <summary>
    /// SuperAdmin always holds every permission and is never edited. Otherwise you edit roles strictly
    /// below your rank, so an Admin can't widen the Admin role (and with it, their own access).
    /// </summary>
    public static Error? CanEditRole(Actor actor, string role)
    {
        if (!AppRoles.IsKnown(role))
            return new Error(ErrorKind.NotFound, "Role not found", $"Role '{role}' does not exist.");

        if (string.Equals(role, AppRoles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            return Error.Forbidden("The SuperAdmin role always holds every permission and can't be changed.");

        if (!actor.IsSuperAdmin && AppRoles.RankOf(role) >= actor.Rank)
            return Error.Forbidden($"You can't change the {role} role: you can only change roles below your own.");

        return null;
    }
}
