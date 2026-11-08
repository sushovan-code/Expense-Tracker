namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Stored refresh tokens (hashes only).</summary>
public interface IRefreshTokenStore
{
    Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);
    Task AddAsync(RefreshTokenRecord token, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the token rotated, atomically: <c>false</c> when it was already revoked, so two concurrent
    /// refreshes can't both rotate it.
    /// </summary>
    Task<bool> TryRotateAsync(Guid tokenId, Guid replacementId, DateTimeOffset now, CancellationToken cancellationToken);

    Task<int> RevokeSessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Revokes every session of the user, except <paramref name="exceptSessionId"/> when given.</summary>
    Task<int> RevokeUserSessionsAsync(Guid userId, DateTimeOffset now, Guid? exceptSessionId, CancellationToken cancellationToken);

    Task DeleteExpiredAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
