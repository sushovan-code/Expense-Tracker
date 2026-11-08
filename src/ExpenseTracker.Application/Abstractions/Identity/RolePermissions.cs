namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>A stored role with its permission claims (as stored, unfiltered) and how many users hold it.</summary>
public sealed record RolePermissions(string Name, IReadOnlyList<string> Permissions, int UserCount);
