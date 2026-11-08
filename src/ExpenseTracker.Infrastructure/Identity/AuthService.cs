using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Infrastructure.Identity;

internal sealed partial class AuthService(
    UserManager<AppUser> users,
    AppDbContext db,
    IAccessTokenIssuer tokens,
    IUserAccessProvider access,
    IOptions<IdentitySettings> settings,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    // One message for unknown email and wrong password, so sign-in can't be used to discover accounts.
    private static readonly Error InvalidCredentials = Error.Unauthorized("Sign-in failed", "Invalid email or password.");
    private static readonly Error InvalidSession = Error.Unauthorized("Session expired", "Your session has expired. Please sign in again.");

    // Verified against when the email is unknown, so both paths cost one password hash.
    private static readonly Lazy<string> DummyPasswordHash =
        new(() => new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString()));

    public async Task<Result<AuthSession>> LoginAsync(LoginCommand command, string? ipAddress, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(command.Email.Trim());
        if (user is null)
        {
            users.PasswordHasher.VerifyHashedPassword(new AppUser(), DummyPasswordHash.Value, command.Password);
            LogLoginFailed(logger, null, ipAddress);
            return InvalidCredentials;
        }

        if (await users.IsLockedOutAsync(user))
        {
            LogLoginLockedOut(logger, user.Id, ipAddress);
            return LockedOut(user);
        }

        if (!await users.CheckPasswordAsync(user, command.Password))
        {
            await users.AccessFailedAsync(user);
            if (await users.IsLockedOutAsync(user))
            {
                LogLoginLockedOut(logger, user.Id, ipAddress);
                return LockedOut(user);
            }

            LogLoginFailed(logger, user.Id, ipAddress);
            return InvalidCredentials;
        }

        // Only revealed after the password checked out.
        if (!user.IsActive)
        {
            LogLoginRejectedInactive(logger, user.Id, ipAddress);
            return Error.Unauthorized("Account disabled", "This account has been disabled. Contact an administrator.");
        }

        var now = clock.GetUtcNow();
        await users.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = now;
        await users.UpdateAsync(user);

        // Housekeeping: this user's long-dead refresh tokens.
        await db.RefreshTokens.Where(t => t.UserId == user.Id && t.ExpiresAt < now).ExecuteDeleteAsync(ct);

        // A password check may have re-hashed the password, which rotates the security stamp.
        await access.InvalidateAsync(user.Id, ct);

        var session = await IssueSessionAsync(user.Id, sessionId: Guid.CreateVersion7(), Guid.CreateVersion7(), ipAddress, ct);
        if (session.IsSuccess) LogLoginSucceeded(logger, user.Id, ipAddress);
        return session;
    }

    public async Task<Result<AuthSession>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken ct)
    {
        var hash = SecureTokens.Hash(refreshToken);
        var token = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null) return InvalidSession;

        var now = clock.GetUtcNow();
        if (token.RevokedAt is not null)
        {
            if (token.ReplacedById is not null)
            {
                // A rotated token was presented again: it was copied. Revoke the whole chain so
                // neither the thief nor the victim can continue; the victim simply signs in again.
                await db.RefreshTokens.RevokeSessionAsync(token.SessionId, now, ct);
                LogRefreshTokenReuse(logger, token.UserId, token.SessionId, ipAddress);
            }
            return InvalidSession;
        }

        if (token.ExpiresAt <= now) return InvalidSession;

        // Claim the token atomically, so two concurrent refreshes can't both rotate it.
        var nextId = Guid.CreateVersion7();
        var claimed = await db.RefreshTokens
            .Where(t => t.Id == token.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.ReplacedById, nextId), ct);
        if (claimed == 0) return InvalidSession;

        var session = await IssueSessionAsync(token.UserId, token.SessionId, nextId, ipAddress, ct);
        if (!session.IsSuccess)
        {
            await db.RefreshTokens.RevokeSessionAsync(token.SessionId, now, ct);
            return session;
        }

        LogSessionRefreshed(logger, token.UserId, token.SessionId);
        return session;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(refreshToken)) return;

        var hash = SecureTokens.Hash(refreshToken);
        var token = await db.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null) return;

        await db.RefreshTokens.RevokeSessionAsync(token.SessionId, clock.GetUtcNow(), ct);
        LogLoggedOut(logger, token.UserId, token.SessionId);
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId, ChangePasswordCommand command, string? currentRefreshToken, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null) return Error.NotFound("User", userId);

        // Validates the new password and rotates the security stamp (invalidating every issued access token).
        var changed = await users.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!changed.Succeeded) return changed.ToError(passwordField: "newPassword");

        Guid? keepSession = null;
        if (!string.IsNullOrEmpty(currentRefreshToken))
        {
            var hash = SecureTokens.Hash(currentRefreshToken);
            keepSession = await db.RefreshTokens.Where(t => t.TokenHash == hash && t.UserId == userId)
                .Select(t => (Guid?)t.SessionId).SingleOrDefaultAsync(ct);
        }

        await db.RefreshTokens.RevokeUserSessionsAsync(userId, clock.GetUtcNow(), ct, exceptSessionId: keepSession);
        await access.InvalidateAsync(userId, ct);
        LogPasswordChanged(logger, userId);
        return Result.Success();
    }

    private async Task<Result<AuthSession>> IssueSessionAsync(
        Guid userId, Guid sessionId, Guid refreshTokenId, string? ipAddress, CancellationToken ct)
    {
        var user = await access.GetAsync(userId, ct);
        if (user is null || !user.IsActive) return InvalidSession;

        var now = clock.GetUtcNow();
        var refreshToken = SecureTokens.Generate();
        var refreshExpiresAt = now + settings.Value.RefreshTokenLifetime;
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = refreshTokenId,
            UserId = userId,
            SessionId = sessionId,
            TokenHash = SecureTokens.Hash(refreshToken),
            CreatedAt = now,
            ExpiresAt = refreshExpiresAt,
            CreatedByIp = ipAddress,
        });
        await db.SaveChangesAsync(ct);

        var accessToken = tokens.Issue(new AccessTokenSubject(userId, user.Email, user.DisplayName, user.SecurityStamp));
        return new AuthSession(accessToken.Token, accessToken.ExpiresAt, refreshToken, refreshExpiresAt, user.ToCurrentUser());
    }

    private Error LockedOut(AppUser user)
    {
        var minutes = user.LockoutEnd is { } end ? Math.Max(1, (int)Math.Ceiling((end - clock.GetUtcNow()).TotalMinutes)) : 15;
        return Error.Unauthorized("Account locked",
            $"Too many failed sign-in attempts. Try again in {minutes} minute{(minutes == 1 ? "" : "s")}.");
    }

    [LoggerMessage(EventId = LogEvents.LoginSucceeded, Level = LogLevel.Information,
        Message = "User {UserId} signed in from {IpAddress}")]
    private static partial void LogLoginSucceeded(ILogger logger, Guid userId, string? ipAddress);

    [LoggerMessage(EventId = LogEvents.LoginFailed, Level = LogLevel.Warning,
        Message = "Failed sign-in for user {UserId} from {IpAddress} (null user id = unknown email)")]
    private static partial void LogLoginFailed(ILogger logger, Guid? userId, string? ipAddress);

    [LoggerMessage(EventId = LogEvents.LoginLockedOut, Level = LogLevel.Warning,
        Message = "Sign-in refused for locked-out user {UserId} from {IpAddress}")]
    private static partial void LogLoginLockedOut(ILogger logger, Guid userId, string? ipAddress);

    [LoggerMessage(EventId = LogEvents.LoginRejectedInactive, Level = LogLevel.Warning,
        Message = "Sign-in refused for disabled user {UserId} from {IpAddress}")]
    private static partial void LogLoginRejectedInactive(ILogger logger, Guid userId, string? ipAddress);

    [LoggerMessage(EventId = LogEvents.SessionRefreshed, Level = LogLevel.Debug,
        Message = "Refreshed session {SessionId} for user {UserId}")]
    private static partial void LogSessionRefreshed(ILogger logger, Guid userId, Guid sessionId);

    [LoggerMessage(EventId = LogEvents.RefreshTokenReuseDetected, Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId} from {IpAddress}; session {SessionId} revoked")]
    private static partial void LogRefreshTokenReuse(ILogger logger, Guid userId, Guid sessionId, string? ipAddress);

    [LoggerMessage(EventId = LogEvents.LoggedOut, Level = LogLevel.Information,
        Message = "User {UserId} signed out of session {SessionId}")]
    private static partial void LogLoggedOut(ILogger logger, Guid userId, Guid sessionId);

    [LoggerMessage(EventId = LogEvents.PasswordChanged, Level = LogLevel.Information,
        Message = "User {UserId} changed their password; other sessions revoked")]
    private static partial void LogPasswordChanged(ILogger logger, Guid userId);
}

/// <summary>Bulk revocation of refresh-token chains.</summary>
internal static class RefreshTokenRevocation
{
    public static Task<int> RevokeSessionAsync(this DbSet<RefreshToken> tokens, Guid sessionId, DateTimeOffset now, CancellationToken ct) =>
        tokens.Where(t => t.SessionId == sessionId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    public static Task<int> RevokeUserSessionsAsync(
        this DbSet<RefreshToken> tokens, Guid userId, DateTimeOffset now, CancellationToken ct, Guid? exceptSessionId = null) =>
        tokens.Where(t => t.UserId == userId && t.RevokedAt == null && t.SessionId != exceptSessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
}
