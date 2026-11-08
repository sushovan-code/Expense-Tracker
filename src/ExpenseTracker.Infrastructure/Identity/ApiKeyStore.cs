using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Personal API keys in EF Core. Scopes are stored as one space-separated column.</summary>
internal sealed class ApiKeyStore(AppDbContext dbContext) : IApiKeyStore
{
    private const char ScopeSeparator = ' ';

    public async Task<IReadOnlyList<ApiKeyRecord>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var keys = await dbContext.ApiKeys.AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
        return keys.Select(ToRecord).ToList();
    }

    public Task<int> CountActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.ApiKeys.CountAsync(k => k.UserId == userId && k.RevokedAt == null && k.ExpiresAt > now, cancellationToken);

    public async Task AddAsync(ApiKeyRecord key, CancellationToken cancellationToken)
    {
        dbContext.ApiKeys.Add(new ApiKey
        {
            Id = key.Id,
            UserId = key.UserId,
            Name = key.Name,
            Prefix = key.Prefix,
            KeyHash = key.KeyHash,
            Scopes = string.Join(ScopeSeparator, key.Scopes),
            CreatedAt = key.CreatedAt,
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            RevokedAt = key.RevokedAt,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ApiKeyRecord?> FindAsync(Guid keyId, Guid userId, CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.AsNoTracking().SingleOrDefaultAsync(k => k.Id == keyId && k.UserId == userId, cancellationToken);
        return key is null ? null : ToRecord(key);
    }

    public async Task<ApiKeyRecord?> FindByHashAsync(string keyHash, CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.AsNoTracking().SingleOrDefaultAsync(k => k.KeyHash == keyHash, cancellationToken);
        return key is null ? null : ToRecord(key);
    }

    public Task RevokeAsync(Guid keyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.ApiKeys.Where(k => k.Id == keyId && k.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.RevokedAt, now), cancellationToken);

    public Task MarkUsedAsync(Guid keyId, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.ApiKeys.Where(k => k.Id == keyId)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, now), cancellationToken);

    private static ApiKeyRecord ToRecord(ApiKey k) => new(
        k.Id, k.UserId, k.Name, k.Prefix, k.KeyHash, k.Scopes.Split(ScopeSeparator, StringSplitOptions.RemoveEmptyEntries),
        k.CreatedAt, k.ExpiresAt, k.LastUsedAt, k.RevokedAt);
}
