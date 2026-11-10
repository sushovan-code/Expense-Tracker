namespace ExpenseTracker.Application.Common;

/// <summary>
/// Stable event ids for structured logs across all layers, grouped by area. They make logs filterable
/// and alertable independently of the message text (e.g. "EventId = 3001" for budget overruns).
/// </summary>
public static class LogEvents
{
    // 1xxx – expenses
    public const int ExpenseCreated = 1001;
    public const int ExpenseUpdated = 1002;
    public const int ExpenseDeleted = 1003;
    public const int ExpensesBulkDeleted = 1004;
    public const int ExpenseNotFound = 1010;
    public const int ExpensesListed = 1020;
    public const int ExpenseSummaryComputed = 1021;
    public const int ExpensesExported = 1030;

    // 2xxx – categories
    public const int CategoryCreated = 2001;
    public const int CategoryUpdated = 2002;
    public const int CategoryDeleted = 2003;
    public const int CategoryNotFound = 2010;
    public const int CategoryInUse = 2011;
    public const int CategoriesListed = 2020;

    // 3xxx – budgets
    public const int BudgetExceeded = 3001;
    public const int BudgetStatusComputed = 3020;

    // 4xxx – realtime (SignalR)
    public const int ClientConnected = 4001;
    public const int ClientDisconnected = 4002;
    public const int BroadcastSent = 4010;
    public const int BroadcastFailed = 4011;

    // 5xxx – HTTP pipeline and platform
    public const int RequestCompleted = 5001;
    public const int RequestValidationFailed = 5002;
    public const int UnhandledException = 5003;
    public const int StartupConfiguration = 5010;
    public const int MigrationsApplied = 5011;
    public const int DatabaseUpToDate = 5012;

    // 6xxx – security audit trail (authentication, sessions, administration)
    public const int LoginSucceeded = 6001;
    public const int LoginFailed = 6002;
    public const int LoginLockedOut = 6003;
    public const int LoginRejectedInactive = 6004;
    public const int SessionRefreshed = 6010;
    public const int RefreshTokenReuseDetected = 6011;
    public const int LoggedOut = 6012;
    public const int PasswordChanged = 6013;
    public const int ApiKeyCreated = 6020;
    public const int ApiKeyRevoked = 6021;
    public const int ApiKeyRejected = 6022;
    public const int UserCreated = 6030;
    public const int UserUpdated = 6031;
    public const int UserRolesChanged = 6032;
    public const int UserClaimsChanged = 6033;
    public const int UserPasswordReset = 6034;
    public const int UserUnlocked = 6035;
    public const int UserSessionsRevoked = 6036;
    public const int UserDeleted = 6037;
    public const int UserRegistered = 6038;
    public const int RolePermissionsChanged = 6040;
    public const int AdministrationDenied = 6050;
    public const int IdentitySeeded = 6060;
    public const int IdentitySeedSkipped = 6061;

    // 6070 – family membership changes
    public const int FamilyChanged = 6070;
}
