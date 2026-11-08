using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Identity;

/// <remarks>
/// Every mutation first checks <see cref="AccessPolicy.CanManageUser"/>: you can't manage yourself or
/// anyone at or above your rank (SuperAdmins excepted). Because only a SuperAdmin can manage a SuperAdmin,
/// and nobody can manage themselves, at least one active SuperAdmin always remains.
/// </remarks>
internal sealed partial class UserAdminService(
    IUserAccountStore accounts,
    IUserCredentialStore credentials,
    IUserGrantStore grants,
    IUserQueries queries,
    IRefreshTokenStore refreshTokens,
    IUserAccessProvider access,
    TimeProvider clock,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    private static readonly Error NoRoles = Error.Validation("roles", "Assign at least one role.");

    public async Task<Result<PagedResult<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken cancellationToken)
    {
        var sort = Paging.ResolveSort(request.SortBy, request.SortDir, UserSorting.Default, defaultDescending: false, UserSorting.Fields);
        if (!sort.IsSuccess) return sort.Error;

        return await queries.ListAsync(request, sort.Value, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await accounts.FindByIdAsync(id, cancellationToken);
        if (user is null) return Error.NotFound("User", id);
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> CreateAsync(Actor actor, CreateUserCommand command, CancellationToken cancellationToken)
    {
        var roles = command.Roles.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (roles.Count == 0) return NoRoles;
        if (Deny(actor, null, AccessPolicy.CanAssignRoles(actor, roles)) is { } denied) return denied;

        var canonicalRoles = roles.Select(Canonical).ToList();
        var account = new NewUserAccount(command.Email.Trim(), command.DisplayName.Trim(), clock.GetUtcNow());
        var created = await accounts.CreateAsync(account, command.Password, canonicalRoles, cancellationToken);
        if (!created.IsSuccess) return created.Error;

        LogUserCreated(logger, actor.UserId, created.Value, string.Join(", ", canonicalRoles));
        return await GetAsync(created.Value, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> UpdateAsync(Actor actor, Guid id, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        var email = command.Email.Trim();
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        var deactivated = user.IsActive && !command.IsActive;

        var updated = await accounts.UpdateProfileAsync(id, email, command.DisplayName.Trim(), command.IsActive, cancellationToken);
        if (!updated.IsSuccess) return updated.Error;

        if (emailChanged || deactivated)
        {
            // Issued access tokens stop working immediately; a disabled user can't refresh either.
            await credentials.RotateSecurityStampAsync(id, cancellationToken);
        }
        if (deactivated)
        {
            await refreshTokens.RevokeUserSessionsAsync(id, clock.GetUtcNow(), exceptSessionId: null, cancellationToken);
        }

        await access.InvalidateAsync(id, cancellationToken);
        LogUserUpdated(logger, actor.UserId, id, emailChanged, command.IsActive);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> SetRolesAsync(Actor actor, Guid id, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;
        var current = target.Value.Roles;

        var requested = roles.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (requested.Count == 0) return NoRoles;

        var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();

        // Removing a role is as sensitive as granting it.
        if (Deny(actor, id, AccessPolicy.CanAssignRoles(actor, toAdd.Concat(toRemove))) is { } denied) return denied;

        if (toRemove.Count > 0 && await grants.RemoveFromRolesAsync(id, toRemove, cancellationToken) is { IsSuccess: false } removed)
            return removed.Error!;
        if (toAdd.Count > 0 && await grants.AddToRolesAsync(id, toAdd.Select(Canonical).ToList(), cancellationToken) is { IsSuccess: false } added)
            return added.Error!;

        await access.InvalidateAsync(id, cancellationToken);
        LogUserRolesChanged(logger, actor.UserId, id, string.Join(", ", current), string.Join(", ", requested.Select(Canonical)));
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> AddClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;

        var normalized = NormalizeClaim(claim);
        if (!normalized.IsSuccess) return normalized.Error;
        var (type, value) = normalized.Value;

        if (type == AppClaimTypes.Permission &&
            Deny(actor, id, AccessPolicy.CanGrantPermissions(actor, [value], field: "value")) is { } denied)
            return denied;

        var existing = await grants.GetClaimsAsync(id, cancellationToken);
        if (!existing.Any(c => c.Type == type && c.Value == value))
        {
            var added = await grants.AddClaimAsync(id, new ClaimDto(type, value), cancellationToken);
            if (!added.IsSuccess) return added.Error!;
            await access.InvalidateAsync(id, cancellationToken);
            LogUserClaimsChanged(logger, actor.UserId, id, "added", type, value);
        }

        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> RemoveClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;

        var normalized = NormalizeClaim(claim);
        if (!normalized.IsSuccess) return normalized.Error;
        var (type, value) = normalized.Value;

        var existing = await grants.GetClaimsAsync(id, cancellationToken);
        if (!existing.Any(c => c.Type == type && c.Value == value))
            return new Error(ErrorKind.NotFound, "Claim not found", $"The user has no '{type}' claim with value '{value}'.");

        var removed = await grants.RemoveClaimAsync(id, new ClaimDto(type, value), cancellationToken);
        if (!removed.IsSuccess) return removed.Error!;

        await access.InvalidateAsync(id, cancellationToken);
        LogUserClaimsChanged(logger, actor.UserId, id, "removed", type, value);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<Result> ResetPasswordAsync(Actor actor, Guid id, string newPassword, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;

        var reset = await credentials.ResetPasswordAsync(id, newPassword, cancellationToken);
        if (!reset.IsSuccess) return reset;

        await refreshTokens.RevokeUserSessionsAsync(id, clock.GetUtcNow(), exceptSessionId: null, cancellationToken);
        await access.InvalidateAsync(id, cancellationToken);
        LogUserPasswordReset(logger, actor.UserId, id);
        return Result.Success();
    }

    public async Task<Result> UnlockAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;

        await credentials.UnlockAsync(id, cancellationToken);
        LogUserUnlocked(logger, actor.UserId, id);
        return Result.Success();
    }

    public async Task<Result> RevokeSessionsAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;

        var revoked = await refreshTokens.RevokeUserSessionsAsync(id, clock.GetUtcNow(), exceptSessionId: null, cancellationToken);
        await credentials.RotateSecurityStampAsync(id, cancellationToken);
        await access.InvalidateAsync(id, cancellationToken);
        LogUserSessionsRevoked(logger, actor.UserId, id, revoked);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id, cancellationToken);
        if (!target.IsSuccess) return target.Error;

        var deleted = await accounts.DeleteAsync(id, cancellationToken);
        if (!deleted.IsSuccess) return deleted;

        await access.InvalidateAsync(id, cancellationToken);
        LogUserDeleted(logger, actor.UserId, id);
        return Result.Success();
    }

    // ---- Helpers --------------------------------------------------------------------------------

    private sealed record Target(UserAccount User, IReadOnlyList<string> Roles);

    private async Task<Result<Target>> LoadManageableAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var user = await accounts.FindByIdAsync(id, cancellationToken);
        if (user is null) return Error.NotFound("User", id);

        var roles = await grants.GetRolesAsync(id, cancellationToken);
        if (Deny(actor, id, AccessPolicy.CanManageUser(actor, id, roles)) is { } denied) return denied;
        return new Target(user, roles);
    }

    private Error? Deny(Actor actor, Guid? targetId, Error? error)
    {
        if (error?.Kind == ErrorKind.Forbidden) LogAdministrationDenied(logger, actor.UserId, targetId, error.Detail);
        return error;
    }

    private static Result<(string Type, string Value)> NormalizeClaim(ClaimDto claim)
    {
        var type = claim.Type.Trim();
        var value = claim.Value.Trim();
        if (type.Length == 0) return Error.Validation("type", "Claim type is required.");
        if (value.Length == 0) return Error.Validation("value", "Claim value is required.");

        // Stored lowercase so it always matches the claim type the access provider reads.
        if (string.Equals(type, AppClaimTypes.Permission, StringComparison.OrdinalIgnoreCase))
            return (AppClaimTypes.Permission, value);

        if (AppClaimTypes.Reserved.Contains(type))
            return Error.Validation("type", $"'{type}' claims are managed by the system and can't be set directly.");

        return (type, value);
    }

    private static string Canonical(string role) => AppRoles.Find(role)?.Name ?? role;

    private async Task<UserDetailDto> ToDetailAsync(UserAccount user, CancellationToken cancellationToken)
    {
        var roles = (await grants.GetRolesAsync(user.Id, cancellationToken)).OrderByDescending(AppRoles.RankOf).ToList();
        var claims = (await grants.GetClaimsAsync(user.Id, cancellationToken))
            .OrderBy(c => c.Type, StringComparer.Ordinal).ThenBy(c => c.Value, StringComparer.Ordinal)
            .ToList();
        var effective = (await access.GetAsync(user.Id, cancellationToken))?.Permissions ?? [];

        return new UserDetailDto(
            user.Id, user.Email, user.DisplayName, roles, user.IsActive,
            user.LockoutEnd > clock.GetUtcNow(), user.LockoutEnd, user.AccessFailedCount,
            user.CreatedAt, user.LastLoginAt, claims, effective);
    }

    [LoggerMessage(EventId = LogEvents.UserCreated, Level = LogLevel.Information,
        Message = "User {ActorId} created user {UserId} with roles [{Roles}]")]
    private static partial void LogUserCreated(ILogger logger, Guid actorId, Guid userId, string roles);

    [LoggerMessage(EventId = LogEvents.UserUpdated, Level = LogLevel.Information,
        Message = "User {ActorId} updated user {UserId} (email changed {EmailChanged}, active {IsActive})")]
    private static partial void LogUserUpdated(ILogger logger, Guid actorId, Guid userId, bool emailChanged, bool isActive);

    [LoggerMessage(EventId = LogEvents.UserRolesChanged, Level = LogLevel.Information,
        Message = "User {ActorId} changed roles of user {UserId}: [{PreviousRoles}] → [{Roles}]")]
    private static partial void LogUserRolesChanged(ILogger logger, Guid actorId, Guid userId, string previousRoles, string roles);

    [LoggerMessage(EventId = LogEvents.UserClaimsChanged, Level = LogLevel.Information,
        Message = "User {ActorId} {ClaimChange} claim {ClaimType}={ClaimValue} on user {UserId}")]
    private static partial void LogUserClaimsChanged(ILogger logger, Guid actorId, Guid userId, string claimChange, string claimType, string claimValue);

    [LoggerMessage(EventId = LogEvents.UserPasswordReset, Level = LogLevel.Information,
        Message = "User {ActorId} reset the password of user {UserId}; their sessions were revoked")]
    private static partial void LogUserPasswordReset(ILogger logger, Guid actorId, Guid userId);

    [LoggerMessage(EventId = LogEvents.UserUnlocked, Level = LogLevel.Information,
        Message = "User {ActorId} unlocked user {UserId}")]
    private static partial void LogUserUnlocked(ILogger logger, Guid actorId, Guid userId);

    [LoggerMessage(EventId = LogEvents.UserSessionsRevoked, Level = LogLevel.Information,
        Message = "User {ActorId} signed user {UserId} out everywhere ({RevokedCount} refresh token(s) revoked)")]
    private static partial void LogUserSessionsRevoked(ILogger logger, Guid actorId, Guid userId, int revokedCount);

    [LoggerMessage(EventId = LogEvents.UserDeleted, Level = LogLevel.Information,
        Message = "User {ActorId} deleted user {UserId}")]
    private static partial void LogUserDeleted(ILogger logger, Guid actorId, Guid userId);

    [LoggerMessage(EventId = LogEvents.AdministrationDenied, Level = LogLevel.Warning,
        Message = "Denied administrative action by user {ActorId} on user {TargetUserId}: {Reason}")]
    private static partial void LogAdministrationDenied(ILogger logger, Guid actorId, Guid? targetUserId, string reason);
}
