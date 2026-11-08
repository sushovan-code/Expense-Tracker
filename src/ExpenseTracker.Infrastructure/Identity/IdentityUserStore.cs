using System.Security.Claims;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// Adapter for the user ports over ASP.NET Core Identity's <see cref="UserManager{TUser}"/>: hashing, password
/// validation, lockout counting and the security stamp stay Identity's job. Within one request scope the
/// manager returns the same tracked user, so repeated lookups don't hit the database again.
/// </summary>
internal sealed class IdentityUserStore(UserManager<AppUser> users, AppDbContext dbContext)
    : IUserAccountStore, IUserCredentialStore, IUserGrantStore
{
    private const string NewPasswordField = "newPassword";

    // Verified against when the email is unknown, so both paths cost one password hash.
    private static readonly Lazy<string> DummyPasswordHash =
        new(() => new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString()));

    // ---- Accounts -------------------------------------------------------------------------------

    public async Task<UserAccount?> FindByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        ToAccount(await users.FindByIdAsync(userId.ToString()));

    public async Task<UserAccount?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        ToAccount(await users.FindByEmailAsync(email));

    public async Task<Result<Guid>> CreateAsync(
        NewUserAccount account, string password, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        var user = new AppUser
        {
            UserName = account.Email,
            Email = account.Email,
            EmailConfirmed = true,
            DisplayName = account.DisplayName,
            IsActive = true,
            CreatedAt = account.CreatedAt,
        };

        // The account and its roles are saved together, so a failed role assignment leaves no half-created user.
        var failure = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
            {
                return created.ToError();
            }

            var assigned = await users.AddToRolesAsync(user, roles);
            if (!assigned.Succeeded)
            {
                return assigned.ToError();
            }

            await transaction.CommitAsync(cancellationToken);
            return (Error?)null;
        });

        return failure is null ? user.Id : failure;
    }

    public async Task<Result> UpdateProfileAsync(Guid userId, string email, string displayName, bool isActive, CancellationToken cancellationToken)
    {
        var user = await RequireAsync(userId);
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);

        user.DisplayName = displayName;
        user.IsActive = isActive;
        if (emailChanged)
        {
            user.Email = email;
            user.UserName = email;
            await users.UpdateNormalizedEmailAsync(user);
            await users.UpdateNormalizedUserNameAsync(user);
        }

        return ToResult(await users.UpdateAsync(user));
    }

    public async Task<Result> DeleteAsync(Guid userId, CancellationToken cancellationToken) =>
        ToResult(await users.DeleteAsync(await RequireAsync(userId)));

    // ---- Credentials ----------------------------------------------------------------------------

    public void SimulatePasswordCheck(string password) =>
        users.PasswordHasher.VerifyHashedPassword(new AppUser(), DummyPasswordHash.Value, password);

    public async Task<LockoutStatus> GetLockoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await RequireAsync(userId);
        return new LockoutStatus(await users.IsLockedOutAsync(user), user.LockoutEnd);
    }

    public async Task<bool> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken) =>
        await users.CheckPasswordAsync(await RequireAsync(userId), password);

    public async Task RecordFailedSignInAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.AccessFailedAsync(await RequireAsync(userId));

    public async Task RecordSuccessfulSignInAsync(Guid userId, DateTimeOffset signedInAt, CancellationToken cancellationToken)
    {
        var user = await RequireAsync(userId);
        await users.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = signedInAt;
        await users.UpdateAsync(user);
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var changed = await users.ChangePasswordAsync(await RequireAsync(userId), currentPassword, newPassword);
        return changed.Succeeded ? Result.Success() : changed.ToError(passwordField: NewPasswordField);
    }

    public async Task<Result> ResetPasswordAsync(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        var user = await RequireAsync(userId);

        // Validate before touching the hash, so a rejected password never leaves the account without one.
        var errors = new List<IdentityError>();
        foreach (var validator in users.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(users, user, newPassword);
            errors.AddRange(validation.Errors);
        }
        if (errors.Count > 0) return IdentityResultErrors.ToError(errors, passwordField: NewPasswordField);

        user.PasswordHash = users.PasswordHasher.HashPassword(user, newPassword);
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await users.UpdateSecurityStampAsync(user); // saves the user, and invalidates issued access tokens
        return Result.Success();
    }

    public async Task UnlockAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await RequireAsync(userId);
        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
    }

    public async Task RotateSecurityStampAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.UpdateSecurityStampAsync(await RequireAsync(userId));

    // ---- Roles and claims -----------------------------------------------------------------------

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken cancellationToken) =>
        (await users.GetRolesAsync(await RequireAsync(userId))).ToList();

    public async Task<Result> AddToRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken) =>
        ToResult(await users.AddToRolesAsync(await RequireAsync(userId), roles));

    public async Task<Result> RemoveFromRolesAsync(Guid userId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken) =>
        ToResult(await users.RemoveFromRolesAsync(await RequireAsync(userId), roles));

    public async Task<IReadOnlyList<ClaimDto>> GetClaimsAsync(Guid userId, CancellationToken cancellationToken) =>
        (await users.GetClaimsAsync(await RequireAsync(userId))).Select(c => new ClaimDto(c.Type, c.Value)).ToList();

    public async Task<Result> AddClaimAsync(Guid userId, ClaimDto claim, CancellationToken cancellationToken) =>
        ToResult(await users.AddClaimAsync(await RequireAsync(userId), new Claim(claim.Type, claim.Value)));

    public async Task<Result> RemoveClaimAsync(Guid userId, ClaimDto claim, CancellationToken cancellationToken) =>
        ToResult(await users.RemoveClaimAsync(await RequireAsync(userId), new Claim(claim.Type, claim.Value)));

    // ---- Helpers --------------------------------------------------------------------------------

    /// <summary>The use cases look the user up first, so a missing user here is a bug, not an expected failure.</summary>
    private async Task<AppUser> RequireAsync(Guid userId) =>
        await users.FindByIdAsync(userId.ToString())
        ?? throw new InvalidOperationException($"User {userId} does not exist.");

    private static Result ToResult(IdentityResult result) => result.Succeeded ? Result.Success() : result.ToError();

    private static UserAccount? ToAccount(AppUser? user) => user is null
        ? null
        : new UserAccount(user.Id, user.Email ?? "", user.DisplayName, user.IsActive, user.LockoutEnd,
            user.AccessFailedCount, user.CreatedAt, user.LastLoginAt);
}
