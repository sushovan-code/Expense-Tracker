using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Auth;

/// <summary>Requires every listed permission claim.</summary>
internal sealed class PermissionRequirement(IReadOnlyList<string> permissions) : IAuthorizationRequirement
{
    public IReadOnlyList<string> Permissions { get; } = permissions;

    public override string ToString() => $"PermissionRequirement: {string.Join(", ", Permissions)}";
}
