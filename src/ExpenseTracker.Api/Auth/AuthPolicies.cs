namespace ExpenseTracker.Api.Auth;

internal static class AuthPolicies
{
    /// <summary>Signed in with a role of Admin or higher.</summary>
    public const string AdminOrAbove = nameof(AdminOrAbove);

    /// <summary>Signed in interactively (JWT). API keys can't manage credentials such as passwords and other API keys.</summary>
    public const string Interactive = nameof(Interactive);
}
