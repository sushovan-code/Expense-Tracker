using System.Security.Claims;
using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Identity;

/// <remarks>
/// Every mutation first checks <see cref="AccessPolicy.CanManageUser"/>: you can't manage yourself or
/// anyone at or above your rank (SuperAdmins excepted). Because only a SuperAdmin can manage a SuperAdmin,
/// and nobody can manage themselves, at least one active SuperAdmin always remains.
/// </remarks>
internal sealed partial class UserAdminService(
    UserManager<AppUser> users,
    AppDbContext dbContext,
    IUserAccessProvider access,
    TimeProvider clock,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    public async Task<Result<PagedResult<UserSummaryDto>>> ListAsync(UserListRequest request, CancellationToken cancellationToken)
    {
        var sort = Paging.ResolveSort(request.SortBy, request.SortDir, UserSorting.Default, defaultDescending: false, UserSorting.Fields);
        if (!sort.IsSuccess) return sort.Error;

        var query = FilterUsers(request);
        var total = await query.CountAsync(cancellationToken);
        var (page, pageSize) = Paging.Resolve(request.Page, total);

        var rows = await SortUsers(query, sort.Value).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.DisplayName, u.IsActive, u.LockoutEnd, u.CreatedAt, u.LastLoginAt })
            .ToListAsync(cancellationToken);

        var roles = await RolesByUserAsync(rows.Select(r => r.Id).ToList(), cancellationToken);
        var now = clock.GetUtcNow();
        var items = rows.Select(r => new UserSummaryDto(
            r.Id, r.Email ?? "", r.DisplayName, roles.GetValueOrDefault(r.Id) ?? [], r.IsActive, r.LockoutEnd > now, r.CreatedAt, r.LastLoginAt))
            .ToList();

        return new PagedResult<UserSummaryDto>(items, page, pageSize, total);
    }

    private IQueryable<AppUser> FilterUsers(UserListRequest request)
    {
        IQueryable<AppUser> query = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = LikePatterns.Contains(request.Search.Trim());
            query = query.Where(u => EF.Functions.Like(u.Email!, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.Like(u.DisplayName, pattern, LikePatterns.EscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim().ToUpperInvariant();
            query = query.Where(u => dbContext.UserRoles.Any(ur => ur.UserId == u.Id && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == role)));
        }

        return query;
    }

    private static IOrderedQueryable<AppUser> SortUsers(IQueryable<AppUser> query, SortSpec sort) => (sort.Field, sort.Descending) switch
    {
        ("email", false) => query.OrderBy(u => u.Email),
        ("email", true) => query.OrderByDescending(u => u.Email),
        ("created", false) => query.OrderBy(u => u.CreatedAt),
        ("created", true) => query.OrderByDescending(u => u.CreatedAt),
        ("lastlogin", false) => query.OrderBy(u => u.LastLoginAt),
        ("lastlogin", true) => query.OrderByDescending(u => u.LastLoginAt),
        (_, true) => query.OrderByDescending(u => u.DisplayName),
        _ => query.OrderBy(u => u.DisplayName),
    };

    public async Task<Result<UserDetailDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Error.NotFound("User", id);
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> CreateAsync(Actor actor, CreateUserCommand command, CancellationToken cancellationToken)
    {
        var roles = command.Roles.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (roles.Count == 0) return Error.Validation("roles", "Assign at least one role.");
        if (Deny(actor, null, AccessPolicy.CanAssignRoles(actor, roles)) is { } denied) return denied;

        var email = command.Email.Trim();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = command.DisplayName.Trim(),
            IsActive = true,
            CreatedAt = clock.GetUtcNow(),
        };

        // The account and its roles are saved together, so a failed role assignment leaves no half-created user.
        var saved = await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var created = await users.CreateAsync(user, command.Password);
            if (!created.Succeeded)
            {
                return created.ToError();
            }

            var assigned = await users.AddToRolesAsync(user, roles.Select(Canonical));
            if (!assigned.Succeeded)
            {
                return assigned.ToError();
            }

            await transaction.CommitAsync(cancellationToken);
            return (Error?)null;
        });
        if (saved is not null)
        {
            return saved;
        }

        LogUserCreated(logger, actor.UserId, user.Id, string.Join(", ", roles.Select(Canonical)));
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> UpdateAsync(Actor actor, Guid id, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        var email = command.Email.Trim();
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        var deactivated = user.IsActive && !command.IsActive;

        user.DisplayName = command.DisplayName.Trim();
        user.IsActive = command.IsActive;
        if (emailChanged)
        {
            user.Email = email;
            user.UserName = email;
            await users.UpdateNormalizedEmailAsync(user);
            await users.UpdateNormalizedUserNameAsync(user);
        }

        var updated = await users.UpdateAsync(user);
        if (!updated.Succeeded) return updated.ToError();

        if (emailChanged || deactivated)
        {
            // Issued access tokens stop working immediately; a disabled user can't refresh either.
            await users.UpdateSecurityStampAsync(user);
        }
        if (deactivated)
        {
            await dbContext.RefreshTokens.RevokeUserSessionsAsync(user.Id, clock.GetUtcNow(), cancellationToken);
        }

        await access.InvalidateAsync(user.Id, cancellationToken);
        LogUserUpdated(logger, actor.UserId, user.Id, emailChanged, user.IsActive);
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> SetRolesAsync(Actor actor, Guid id, IReadOnlyList<string> roles, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var (user, current) = target.Value;

        var requested = roles.Select(r => r.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (requested.Count == 0) return Error.Validation("roles", "Assign at least one role.");

        var toAdd = requested.Except(current, StringComparer.OrdinalIgnoreCase).ToList();
        var toRemove = current.Except(requested, StringComparer.OrdinalIgnoreCase).ToList();

        // Removing a role is as sensitive as granting it.
        if (Deny(actor, id, AccessPolicy.CanAssignRoles(actor, toAdd.Concat(toRemove))) is { } denied) return denied;

        if (toRemove.Count > 0)
        {
            var removed = await users.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded) return removed.ToError();
        }
        if (toAdd.Count > 0)
        {
            var added = await users.AddToRolesAsync(user, toAdd.Select(Canonical));
            if (!added.Succeeded) return added.ToError();
        }

        await access.InvalidateAsync(user.Id, cancellationToken);
        LogUserRolesChanged(logger, actor.UserId, user.Id, string.Join(", ", current), string.Join(", ", requested.Select(Canonical)));
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> AddClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        var normalized = NormalizeClaim(claim);
        if (!normalized.IsSuccess) return normalized.Error;
        var (type, value) = normalized.Value;

        if (type == AppClaimTypes.Permission &&
            Deny(actor, id, AccessPolicy.CanGrantPermissions(actor, [value], field: "value")) is { } denied)
            return denied;

        var existing = await users.GetClaimsAsync(user);
        if (!existing.Any(c => c.Type == type && c.Value == value))
        {
            var added = await users.AddClaimAsync(user, new Claim(type, value));
            if (!added.Succeeded) return added.ToError();
            await access.InvalidateAsync(user.Id, cancellationToken);
            LogUserClaimsChanged(logger, actor.UserId, user.Id, "added", type, value);
        }

        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result<UserDetailDto>> RemoveClaimAsync(Actor actor, Guid id, ClaimDto claim, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        var normalized = NormalizeClaim(claim);
        if (!normalized.IsSuccess) return normalized.Error;
        var (type, value) = normalized.Value;

        var existing = (await users.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == type && c.Value == value);
        if (existing is null)
            return new Error(ErrorKind.NotFound, "Claim not found", $"The user has no '{type}' claim with value '{value}'.");

        var removed = await users.RemoveClaimAsync(user, existing);
        if (!removed.Succeeded) return removed.ToError();

        await access.InvalidateAsync(user.Id, cancellationToken);
        LogUserClaimsChanged(logger, actor.UserId, user.Id, "removed", type, value);
        return await ToDetailAsync(user, cancellationToken);
    }

    public async Task<Result> ResetPasswordAsync(Actor actor, Guid id, string newPassword, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        // Validate before touching the hash, so a rejected password never leaves the account without one.
        var errors = new List<IdentityError>();
        foreach (var validator in users.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(users, user, newPassword);
            errors.AddRange(validation.Errors);
        }
        if (errors.Count > 0) return IdentityResultErrors.ToError(errors, passwordField: "newPassword");

        user.PasswordHash = users.PasswordHasher.HashPassword(user, newPassword);
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await users.UpdateSecurityStampAsync(user); // saves the user, and invalidates issued access tokens

        await dbContext.RefreshTokens.RevokeUserSessionsAsync(user.Id, clock.GetUtcNow(), cancellationToken);
        await access.InvalidateAsync(user.Id, cancellationToken);
        LogUserPasswordReset(logger, actor.UserId, user.Id);
        return Result.Success();
    }

    public async Task<Result> UnlockAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        await users.SetLockoutEndDateAsync(user, null);
        await users.ResetAccessFailedCountAsync(user);
        LogUserUnlocked(logger, actor.UserId, user.Id);
        return Result.Success();
    }

    public async Task<Result> RevokeSessionsAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        var revoked = await dbContext.RefreshTokens.RevokeUserSessionsAsync(user.Id, clock.GetUtcNow(), cancellationToken);
        await users.UpdateSecurityStampAsync(user);
        await access.InvalidateAsync(user.Id, cancellationToken);
        LogUserSessionsRevoked(logger, actor.UserId, user.Id, revoked);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var target = await LoadManageableAsync(actor, id);
        if (!target.IsSuccess) return target.Error;
        var user = target.Value.User;

        // Refresh tokens, API keys, claims and role links are removed by cascade.
        var deleted = await users.DeleteAsync(user);
        if (!deleted.Succeeded) return deleted.ToError();

        await access.InvalidateAsync(id, cancellationToken);
        LogUserDeleted(logger, actor.UserId, id);
        return Result.Success();
    }

    // ---- Helpers --------------------------------------------------------------------------------

    private sealed record Target(AppUser User, IList<string> Roles);

    private async Task<Result<Target>> LoadManageableAsync(Actor actor, Guid id)
    {
        var user = await users.FindByIdAsync(id.ToString());
        if (user is null) return Error.NotFound("User", id);

        var roles = await users.GetRolesAsync(user);
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

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> RolesByUserAsync(List<Guid> userIds, CancellationToken cancellationToken)
    {
        var pairs = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name }).ToListAsync(cancellationToken);

        return pairs.GroupBy(p => p.UserId).ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<string>)g.Select(p => p.Name!).OrderByDescending(AppRoles.RankOf).ToList());
    }

    private async Task<UserDetailDto> ToDetailAsync(AppUser user, CancellationToken cancellationToken)
    {
        var roles = (await users.GetRolesAsync(user)).OrderByDescending(AppRoles.RankOf).ToList();
        var claims = (await users.GetClaimsAsync(user))
            .Select(c => new ClaimDto(c.Type, c.Value))
            .OrderBy(c => c.Type, StringComparer.Ordinal).ThenBy(c => c.Value, StringComparer.Ordinal)
            .ToList();
        var effective = (await access.GetAsync(user.Id, cancellationToken))?.Permissions ?? [];

        return new UserDetailDto(
            user.Id, user.Email ?? "", user.DisplayName, roles, user.IsActive,
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
