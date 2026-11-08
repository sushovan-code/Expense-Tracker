using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Refresh tokens in EF Core. Revocations are set-based updates, so they're atomic and don't load rows.</summary>
internal sealed class RefreshTokenStore(AppDbContext dbContext) : IRefreshTokenStore
{
    public async Task<RefreshTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        var token = await dbContext.RefreshTokens.AsNoTracking().SingleOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
        return token is null
            ? null
            : new RefreshTokenRecord(token.Id, token.UserId, token.SessionId, token.TokenHash, token.CreatedAt, token.ExpiresAt,
                token.RevokedAt, token.ReplacedById, token.CreatedByIp);
    }

    public async Task AddAsync(RefreshTokenRecord token, CancellationToken cancellationToken)
    {
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = token.Id,
            UserId = token.UserId,
            SessionId = token.SessionId,
            TokenHash = token.TokenHash,
            CreatedAt = token.CreatedAt,
            ExpiresAt = token.ExpiresAt,
            RevokedAt = token.RevokedAt,
            ReplacedById = token.ReplacedById,
            CreatedByIp = token.CreatedByIp,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryRotateAsync(Guid tokenId, Guid replacementId, DateTimeOffset now, CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now).SetProperty(t => t.ReplacedById, replacementId), cancellationToken) > 0;

    public Task<int> RevokeSessionAsync(Guid sessionId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.Where(t => t.SessionId == sessionId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);

    public Task<int> RevokeUserSessionsAsync(Guid userId, DateTimeOffset now, Guid? exceptSessionId, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null && t.SessionId != exceptSessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), cancellationToken);

    public Task DeleteExpiredAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.RefreshTokens.Where(t => t.UserId == userId && t.ExpiresAt < now).ExecuteDeleteAsync(cancellationToken);
}
