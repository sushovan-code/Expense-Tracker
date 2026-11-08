using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Identity;

internal sealed partial class AuthService(
    IUserAccountStore accounts,
    IUserCredentialStore credentials,
    IRefreshTokenStore refreshTokens,
    IAccessTokenIssuer tokens,
    IUserAccessProvider access,
    SessionPolicy sessionPolicy,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    // One message for unknown email and wrong password, so sign-in can't be used to discover accounts.
    private static readonly Error InvalidCredentials = Error.Unauthorized("Sign-in failed", "Invalid email or password.");
    private static readonly Error InvalidSession = Error.Unauthorized("Session expired", "Your session has expired. Please sign in again.");
    private static readonly Error AccountDisabled = Error.Unauthorized("Account disabled", "This account has been disabled. Contact an administrator.");

    public async Task<Result<AuthSession>> LoginAsync(LoginCommand command, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await accounts.FindByEmailAsync(command.Email.Trim(), cancellationToken);
        if (user is null)
        {
            credentials.SimulatePasswordCheck(command.Password);
            LogLoginFailed(logger, null, ipAddress);
            return InvalidCredentials;
        }

        if (await CheckCredentialsAsync(user.Id, command.Password, ipAddress, cancellationToken) is { } refused)
        {
            return refused;
        }

        // Only revealed after the password checked out.
        if (!user.IsActive)
        {
            LogLoginRejectedInactive(logger, user.Id, ipAddress);
            return AccountDisabled;
        }

        var now = clock.GetUtcNow();
        await credentials.RecordSuccessfulSignInAsync(user.Id, now, cancellationToken);

        // Housekeeping: this user's long-dead refresh tokens.
        await refreshTokens.DeleteExpiredAsync(user.Id, now, cancellationToken);

        // A password check may have re-hashed the password, which rotates the security stamp.
        await access.InvalidateAsync(user.Id, cancellationToken);

        var session = await IssueSessionAsync(user.Id, sessionId: Guid.CreateVersion7(), Guid.CreateVersion7(), ipAddress, cancellationToken);
        if (session.IsSuccess) LogLoginSucceeded(logger, user.Id, ipAddress);
        return session;
    }

    public async Task<Result<AuthSession>> RefreshAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken)
    {
        var token = await refreshTokens.FindByHashAsync(SecureTokens.Hash(refreshToken), cancellationToken);
        if (token is null) return InvalidSession;

        var now = clock.GetUtcNow();
        if (token.RevokedAt is not null)
        {
            if (token.ReplacedById is not null)
            {
                // A rotated token was presented again: it was copied. Revoke the whole chain so
                // neither the thief nor the victim can continue; the victim simply signs in again.
                await refreshTokens.RevokeSessionAsync(token.SessionId, now, cancellationToken);
                LogRefreshTokenReuse(logger, token.UserId, token.SessionId, ipAddress);
            }
            return InvalidSession;
        }

        if (token.ExpiresAt <= now) return InvalidSession;

        var nextId = Guid.CreateVersion7();
        if (!await refreshTokens.TryRotateAsync(token.Id, nextId, now, cancellationToken)) return InvalidSession;

        var session = await IssueSessionAsync(token.UserId, token.SessionId, nextId, ipAddress, cancellationToken);
        if (!session.IsSuccess)
        {
            await refreshTokens.RevokeSessionAsync(token.SessionId, now, cancellationToken);
            return session;
        }

        LogSessionRefreshed(logger, token.UserId, token.SessionId);
        return session;
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken)) return;

        var token = await refreshTokens.FindByHashAsync(SecureTokens.Hash(refreshToken), cancellationToken);
        if (token is null) return;

        await refreshTokens.RevokeSessionAsync(token.SessionId, clock.GetUtcNow(), cancellationToken);
        LogLoggedOut(logger, token.UserId, token.SessionId);
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId, ChangePasswordCommand command, string? currentRefreshToken, CancellationToken cancellationToken)
    {
        if (await accounts.FindByIdAsync(userId, cancellationToken) is null) return Error.NotFound("User", userId);

        // Validates the new password and rotates the security stamp (invalidating every issued access token).
        var changed = await credentials.ChangePasswordAsync(userId, command.CurrentPassword, command.NewPassword, cancellationToken);
        if (!changed.IsSuccess) return changed;

        Guid? keepSession = null;
        if (!string.IsNullOrEmpty(currentRefreshToken)
            && await refreshTokens.FindByHashAsync(SecureTokens.Hash(currentRefreshToken), cancellationToken) is { } current
            && current.UserId == userId)
        {
            keepSession = current.SessionId;
        }

        await refreshTokens.RevokeUserSessionsAsync(userId, clock.GetUtcNow(), keepSession, cancellationToken);
        await access.InvalidateAsync(userId, cancellationToken);
        LogPasswordChanged(logger, userId);
        return Result.Success();
    }

    /// <summary>Lockout and password checks, in that order; <c>null</c> when the password is right.</summary>
    private async Task<Error?> CheckCredentialsAsync(Guid userId, string password, string? ipAddress, CancellationToken cancellationToken)
    {
        if (await credentials.GetLockoutAsync(userId, cancellationToken) is { IsLockedOut: true } locked)
        {
            LogLoginLockedOut(logger, userId, ipAddress);
            return LockedOut(locked);
        }

        if (await credentials.CheckPasswordAsync(userId, password, cancellationToken)) return null;

        await credentials.RecordFailedSignInAsync(userId, cancellationToken);
        if (await credentials.GetLockoutAsync(userId, cancellationToken) is { IsLockedOut: true } nowLocked)
        {
            LogLoginLockedOut(logger, userId, ipAddress);
            return LockedOut(nowLocked);
        }

        LogLoginFailed(logger, userId, ipAddress);
        return InvalidCredentials;
    }

    private async Task<Result<AuthSession>> IssueSessionAsync(
        Guid userId, Guid sessionId, Guid refreshTokenId, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await access.GetAsync(userId, cancellationToken);
        if (user is null || !user.IsActive) return InvalidSession;

        var now = clock.GetUtcNow();
        var refreshToken = SecureTokens.Generate();
        var refreshExpiresAt = now + sessionPolicy.RefreshTokenLifetime;
        await refreshTokens.AddAsync(
            new RefreshTokenRecord(refreshTokenId, userId, sessionId, SecureTokens.Hash(refreshToken), now, refreshExpiresAt,
                RevokedAt: null, ReplacedById: null, ipAddress),
            cancellationToken);

        var accessToken = tokens.Issue(new AccessTokenSubject(userId, user.Email, user.DisplayName, user.SecurityStamp));
        return new AuthSession(accessToken.Token, accessToken.ExpiresAt, refreshToken, refreshExpiresAt, user.ToCurrentUser());
    }

    private Error LockedOut(LockoutStatus lockout)
    {
        var minutes = lockout.End is { } end
            ? Math.Max(1, (int)Math.Ceiling((end - clock.GetUtcNow()).TotalMinutes))
            : (int)IdentityRules.LockoutDuration.TotalMinutes;
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
