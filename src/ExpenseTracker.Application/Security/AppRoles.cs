namespace ExpenseTracker.Application.Security;

/// <summary>
/// The built-in roles, highest first. Roles are coarse groupings of permissions plus a rank: the rank
/// decides who may manage whom (see <see cref="AccessPolicy"/>), the permissions decide what each role can do.
/// </summary>
public static class AppRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Reader = "Reader";

    private static readonly string[] ReadData =
    [
        Permissions.Expenses.View, Permissions.Expenses.Export, Permissions.Categories.View, Permissions.Budgets.View,
    ];

    private static readonly string[] WriteData =
    [
        Permissions.Expenses.Add, Permissions.Expenses.Edit, Permissions.Expenses.Delete,
        Permissions.Categories.Add, Permissions.Categories.Edit, Permissions.Categories.Delete,
    ];

    private static readonly string[] Administration =
    [
        Permissions.Users.View, Permissions.Users.Manage, Permissions.Roles.View, Permissions.Roles.Manage,
    ];

    public static IReadOnlyList<RoleDefinition> All { get; } =
    [
        new(SuperAdmin, 400, "Unrestricted. Always holds every permission; can manage every user, including other administrators.",
            Permissions.All.Select(p => p.Name).ToArray()),
        new(Admin, 300, "Manages users and the permissions of the roles below it, plus everything a User can do.",
            [.. ReadData, .. WriteData, .. Administration, Permissions.ApiKeys.Manage]),
        new(User, 200, "Records and manages expenses, categories and budgets.",
            [.. ReadData, .. WriteData, Permissions.ApiKeys.Manage]),
        new(Reader, 100, "Read-only access to expenses, categories and budgets.",
            [.. ReadData, Permissions.ApiKeys.Manage]),
    ];

    private static readonly Dictionary<string, RoleDefinition> ByName =
        All.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

    public static bool IsKnown(string role) => ByName.ContainsKey(role);

    public static RoleDefinition? Find(string role) => ByName.GetValueOrDefault(role);

    /// <summary>The rank of a role; 0 for unknown roles.</summary>
    public static int RankOf(string role) => ByName.TryGetValue(role, out var definition) ? definition.Rank : 0;

    /// <summary>The highest rank among <paramref name="roles"/>; 0 when there are none.</summary>
    public static int HighestRank(IEnumerable<string> roles) => roles.Select(RankOf).DefaultIfEmpty(0).Max();
}
