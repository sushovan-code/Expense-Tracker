namespace ExpenseTracker.Application.Security;

/// <summary>One grantable permission, as shown in the admin UI.</summary>
public sealed record PermissionInfo(string Name, string Group, string Description);
