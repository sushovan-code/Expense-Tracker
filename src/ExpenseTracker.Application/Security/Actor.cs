using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Security;

/// <summary>The authenticated user performing an administrative action.</summary>
public sealed record Actor(Guid UserId, IReadOnlyCollection<string> Roles, IReadOnlySet<string> Permissions)
{
    public int Rank => AppRoles.HighestRank(Roles);

    public bool IsSuperAdmin => Roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase);

    public bool Has(string permission) => Permissions.Contains(permission);
}
