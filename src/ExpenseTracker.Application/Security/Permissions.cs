namespace ExpenseTracker.Application.Security;

/// <summary>One grantable permission, as shown in the admin UI.</summary>
public sealed record PermissionInfo(string Name, string Group, string Description);

/// <summary>
/// The permission catalog: the single source of truth for what can be granted. Endpoints require
/// permissions (never roles) so a role's abilities can be changed by editing its permission set
/// without touching code. Names are <c>resource.action</c> and are stored as <c>permission</c> claims.
/// </summary>
public static class Permissions
{
    public static class Expenses
    {
        public const string View = "expense.view";
        public const string Add = "expense.add";
        public const string Edit = "expense.edit";
        public const string Delete = "expense.delete";
        public const string Export = "expense.export";
    }

    public static class Categories
    {
        public const string View = "category.view";
        public const string Add = "category.add";
        public const string Edit = "category.edit";
        public const string Delete = "category.delete";
    }

    public static class Budgets
    {
        public const string View = "budget.view";
    }

    public static class Users
    {
        public const string View = "user.view";
        public const string Manage = "user.manage";
    }

    public static class Roles
    {
        public const string View = "role.view";
        public const string Manage = "role.manage";
    }

    public static class ApiKeys
    {
        public const string Manage = "apikey.manage";
    }

    public static IReadOnlyList<PermissionInfo> All { get; } =
    [
        new(Expenses.View, "Expenses", "View expenses and spending summaries"),
        new(Expenses.Add, "Expenses", "Add expenses"),
        new(Expenses.Edit, "Expenses", "Edit expenses"),
        new(Expenses.Delete, "Expenses", "Delete expenses (including bulk delete)"),
        new(Expenses.Export, "Expenses", "Export expenses to CSV"),
        new(Categories.View, "Categories", "View categories"),
        new(Categories.Add, "Categories", "Add categories"),
        new(Categories.Edit, "Categories", "Edit categories and their monthly budgets"),
        new(Categories.Delete, "Categories", "Delete categories"),
        new(Budgets.View, "Budgets", "View budget status"),
        new(Users.View, "Users", "View users, their roles and claims"),
        new(Users.Manage, "Users", "Create, edit, lock and delete users; assign roles and claims"),
        new(Roles.View, "Roles", "View roles and their permissions"),
        new(Roles.Manage, "Roles", "Change the permissions granted to roles"),
        new(ApiKeys.Manage, "API keys", "Create and revoke personal API keys"),
    ];

    private static readonly HashSet<string> Known = All.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);

    public static bool IsKnown(string permission) => Known.Contains(permission);
}
