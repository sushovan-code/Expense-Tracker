using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Infrastructure.Identity;

/// <remarks>
/// Keys look like <c>etk_&lt;prefix&gt;_&lt;secret&gt;</c>: a recognisable marker (so secret scanners and
/// people can spot a leaked key), a short public prefix to tell keys apart, and 256 bits of secret.
/// Keys always expire, may be narrowed to a subset of the owner's permissions, and act as their owner,
/// so disabling the owner or removing a role takes effect on the owner's keys as well.
/// </remarks>
internal sealed partial class ApiKeyService(
    AppDbContext dbContext,
    TimeProvider clock,
    ILogger<ApiKeyService> logger) : IApiKeyService
{
    private const string Marker = "etk_";
    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId, CancellationToken cancellationToken)
    {
        var keys = await dbContext.ApiKeys.AsNoTracking()
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync(cancellationToken);
        return keys.Select(ToDto).ToList();
    }

    public async Task<Result<CreatedApiKeyDto>> CreateAsync(Actor actor, CreateApiKeyCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        if (name.Length is 0 or > 100) return Error.Validation("name", "Name must be 1–100 characters.");
        if (command.ExpiresInDays is < CredentialLimits.ApiKeyMinLifetimeDays or > CredentialLimits.ApiKeyMaxLifetimeDays)
            return Error.Validation("expiresInDays", $"Keys must expire within {CredentialLimits.ApiKeyMinLifetimeDays}–{CredentialLimits.ApiKeyMaxLifetimeDays} days.");

        var scopes = (command.Scopes ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        if (AccessPolicy.CanGrantPermissions(actor, scopes, field: "scopes") is { } denied) return denied;

        var now = clock.GetUtcNow();
        var active = await dbContext.ApiKeys.CountAsync(k => k.UserId == actor.UserId && k.RevokedAt == null && k.ExpiresAt > now, cancellationToken);
        if (active >= CredentialLimits.MaxActiveApiKeysPerUser)
            return Error.Conflict("Too many API keys", $"You can have at most {CredentialLimits.MaxActiveApiKeysPerUser} active API keys. Revoke one first.");

        var prefix = Marker + SecureTokens.Generate(6);
        var key = $"{prefix}_{SecureTokens.Generate(32)}";
        var entity = new ApiKey
        {
            Id = Guid.CreateVersion7(),
            UserId = actor.UserId,
            Name = name,
            Prefix = prefix,
            KeyHash = SecureTokens.Hash(key),
            Scopes = string.Join(' ', scopes),
            CreatedAt = now,
            ExpiresAt = now.AddDays(command.ExpiresInDays),
        };
        dbContext.ApiKeys.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogApiKeyCreated(logger, actor.UserId, entity.Id, prefix, scopes.Count == 0 ? "(all of the owner's)" : entity.Scopes, entity.ExpiresAt);
        return new CreatedApiKeyDto(ToDto(entity), key);
    }

    public async Task<Result> RevokeAsync(Guid userId, Guid keyId, CancellationToken cancellationToken)
    {
        var key = await dbContext.ApiKeys.SingleOrDefaultAsync(k => k.Id == keyId && k.UserId == userId, cancellationToken);
        if (key is null) return Error.NotFound("API key", keyId);
        if (key.RevokedAt is not null) return Result.Success();

        key.RevokedAt = clock.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);
        LogApiKeyRevoked(logger, userId, key.Id, key.Prefix);
        return Result.Success();
    }

    public async Task<ApiKeyIdentity?> ValidateAsync(string key, CancellationToken cancellationToken)
    {
        if (!key.StartsWith(Marker, StringComparison.Ordinal) || key.Length > 200) return null;

        var hash = SecureTokens.Hash(key);
        var match = await dbContext.ApiKeys.AsNoTracking()
            .Where(k => k.KeyHash == hash)
            .Select(k => new { k.Id, k.UserId, k.Scopes, k.ExpiresAt, k.RevokedAt, k.LastUsedAt })
            .SingleOrDefaultAsync(cancellationToken);

        var now = clock.GetUtcNow();
        if (match is null || match.RevokedAt is not null || match.ExpiresAt <= now) return null;

        // Coarse "last used", so authentication doesn't write on every request.
        if (match.LastUsedAt is null || now - match.LastUsedAt > LastUsedResolution)
        {
            await dbContext.ApiKeys.Where(k => k.Id == match.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, now), cancellationToken);
        }

        return new ApiKeyIdentity(match.Id, match.UserId, SplitScopes(match.Scopes));
    }

    private static IReadOnlyList<string> SplitScopes(string scopes) =>
        scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static ApiKeyDto ToDto(ApiKey k) =>
        new(k.Id, k.Name, k.Prefix, SplitScopes(k.Scopes), k.CreatedAt, k.ExpiresAt, k.LastUsedAt, k.RevokedAt);

    [LoggerMessage(EventId = LogEvents.ApiKeyCreated, Level = LogLevel.Information,
        Message = "User {UserId} created API key {ApiKeyId} ({ApiKeyPrefix}) with scopes {Scopes}, expiring {ExpiresAt}")]
    private static partial void LogApiKeyCreated(ILogger logger, Guid userId, Guid apiKeyId, string apiKeyPrefix, string scopes, DateTimeOffset expiresAt);

    [LoggerMessage(EventId = LogEvents.ApiKeyRevoked, Level = LogLevel.Information,
        Message = "User {UserId} revoked API key {ApiKeyId} ({ApiKeyPrefix})")]
    private static partial void LogApiKeyRevoked(ILogger logger, Guid userId, Guid apiKeyId, string apiKeyPrefix);
}
