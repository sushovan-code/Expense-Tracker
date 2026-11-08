namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

// Identity lives in its own "auth" schema with plain table names (instead of dbo.AspNet*),
// keeping it visibly separate from the expense data.

internal static class AuthSchema
{
    public const string Name = "auth";
}
