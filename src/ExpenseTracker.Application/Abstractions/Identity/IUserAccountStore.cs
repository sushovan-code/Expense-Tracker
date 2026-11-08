using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Reads and writes user accounts in the identity store.</summary>
public interface IUserAccountStore
{
    Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    /// <summary>Creates the account with its password and roles in one transaction; nothing is saved on failure.</summary>
    Task<Result<Guid>> CreateAsync(NewUserAccount account, string password, IReadOnlyCollection<string> roles, CancellationToken cancellationToken);

    /// <summary>Changes the email (which is also the user name), display name and active flag.</summary>
    Task<Result> UpdateProfileAsync(Guid userId, string email, string displayName, bool isActive, CancellationToken cancellationToken);

    /// <summary>Deletes the account; its sessions, API keys, claims and role links go with it.</summary>
    Task<Result> DeleteAsync(Guid userId, CancellationToken cancellationToken);
}
