using ExpenseTracker.Application.Security;
using Microsoft.AspNetCore.Authorization;

namespace ExpenseTracker.Api.Auth;

/// <summary>Requires a role ranked at or above <see cref="Role"/> ("Admin or above").</summary>
internal sealed class MinimumRoleRequirement(string role) : IAuthorizationRequirement
{
    public string Role { get; } = role;
    public int Rank { get; } = AppRoles.RankOf(role);

    public override string ToString() => $"MinimumRoleRequirement: {Role} or above";
}
