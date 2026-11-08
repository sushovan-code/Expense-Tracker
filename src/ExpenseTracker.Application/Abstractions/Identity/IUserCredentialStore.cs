using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Passwords, lockout and the security stamp. Hashing and lockout counting stay in the identity store.</summary>
public interface IUserCredentialStore
{
    /// <summary>Spends the same work as a real password check, so an unknown email can't be told apart by timing.</summary>
    void SimulatePasswordCheck(string password);

    Task<LockoutStatus> GetLockoutAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken);

    /// <summary>Counts a failed sign-in; enough of them lock the account out.</summary>
    Task RecordFailedSignInAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Clears the failed-sign-in count and records the sign-in time.</summary>
    Task RecordSuccessfulSignInAsync(Guid userId, DateTimeOffset signedInAt, CancellationToken cancellationToken);

    /// <summary>Checks the current password, validates and sets the new one, and rotates the security stamp.</summary>
    Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);

    /// <summary>Validates and sets a new password, clears any lockout and rotates the security stamp.</summary>
    Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken);

    Task UnlockAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Makes every access token issued so far invalid.</summary>
    Task RotateSecurityStampAsync(Guid userId, CancellationToken cancellationToken);
}
