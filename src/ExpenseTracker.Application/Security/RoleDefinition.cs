namespace ExpenseTracker.Application.Security;

/// <summary>A built-in role. <paramref name="Rank"/> orders roles for "this role or above" checks.</summary>
public sealed record RoleDefinition(string Name, int Rank, string Description, IReadOnlyList<string> DefaultPermissions);
