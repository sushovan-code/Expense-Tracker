namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Password and lockout rules for the identity store, in one place so the messages agree with the policy.</summary>
internal static class IdentityRules
{
    /// <summary>Length over composition (NIST SP 800-63B), while keeping mixed case and a digit. Mirrored in the web app's password policy.</summary>
    public const int MinPasswordLength = 10;

    public const int MaxFailedSignInAttempts = 5;

    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
}
