namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Role → permission grants, stored as role claims.</summary>
public interface IRolePermissionStore
{
    Task<IReadOnlyList<RolePermissions>> ListAsync(CancellationToken cancellationToken);

    /// <summary><c>null</c> when no role has this name (compared without case).</summary>
    Task<RolePermissions?> FindAsync(string role, CancellationToken cancellationToken);

    Task UpdateAsync(string role, IReadOnlyCollection<string> added, IReadOnlyCollection<string> removed, CancellationToken cancellationToken);
}
