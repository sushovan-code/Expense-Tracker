namespace ExpenseTracker.Application.Security;

/// <summary>
/// Limits on passwords and API keys, shared by the HTTP contracts (fast 400s and API docs) and the
/// identity adapters that enforce them. Mirrored in the web app's <c>shared/auth/passwordPolicy.ts</c>.
/// </summary>
public static class CredentialLimits
{
    public const int PasswordMinLength = 10;
    public const int PasswordMaxLength = 128;

    public const int ApiKeyNameMaxLength = 100;
    public const int ApiKeyMinLifetimeDays = 1;
    public const int ApiKeyMaxLifetimeDays = 365;
    public const int ApiKeyDefaultLifetimeDays = 90;
    public const int MaxActiveApiKeysPerUser = 10;
}
