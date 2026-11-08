using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Common;
using Microsoft.Extensions.Logging;

namespace ExpenseTracker.Application.Features.Identity;

/// <remarks>
/// Keys look like <c>etk_&lt;prefix&gt;_&lt;secret&gt;</c>: a recognisable marker (so secret scanners and
/// people can spot a leaked key), a short public prefix to tell keys apart, and 256 bits of secret.
/// Keys always expire, may be narrowed to a subset of the owner's permissions, and act as their owner,
/// so disabling the owner or removing a role takes effect on the owner's keys as well.
/// </remarks>
internal sealed partial class ApiKeyService(
    IApiKeyStore keys,
    TimeProvider clock,
    ILogger<ApiKeyService> logger) : IApiKeyService
{
    private const string Marker = "etk_";
    private const int PrefixEntropyBytes = 6;
    private const int SecretEntropyBytes = 32;
    private const int MaxPresentedKeyLength = 200;
    private static readonly TimeSpan LastUsedResolution = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<ApiKeyDto>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        (await keys.ListAsync(userId, cancellationToken)).Select(ToDto).ToList();

    public async Task<Result<CreatedApiKeyDto>> CreateAsync(Actor actor, CreateApiKeyCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        if (name.Length is 0 or > CredentialLimits.ApiKeyNameMaxLength)
            return Error.Validation("name", $"Name must be 1–{CredentialLimits.ApiKeyNameMaxLength} characters.");
        if (command.ExpiresInDays is < CredentialLimits.ApiKeyMinLifetimeDays or > CredentialLimits.ApiKeyMaxLifetimeDays)
            return Error.Validation("expiresInDays", $"Keys must expire within {CredentialLimits.ApiKeyMinLifetimeDays}–{CredentialLimits.ApiKeyMaxLifetimeDays} days.");

        var scopes = (command.Scopes ?? []).Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        if (AccessPolicy.CanGrantPermissions(actor, scopes, field: "scopes") is { } denied) return denied;

        var now = clock.GetUtcNow();
        if (await keys.CountActiveAsync(actor.UserId, now, cancellationToken) >= CredentialLimits.MaxActiveApiKeysPerUser)
            return Error.Conflict("Too many API keys", $"You can have at most {CredentialLimits.MaxActiveApiKeysPerUser} active API keys. Revoke one first.");

        var prefix = Marker + SecureTokens.Generate(PrefixEntropyBytes);
        var key = $"{prefix}_{SecureTokens.Generate(SecretEntropyBytes)}";
        var record = new ApiKeyRecord(Guid.CreateVersion7(), actor.UserId, name, prefix, SecureTokens.Hash(key), scopes,
            now, now.AddDays(command.ExpiresInDays), LastUsedAt: null, RevokedAt: null);
        await keys.AddAsync(record, cancellationToken);

        LogApiKeyCreated(logger, actor.UserId, record.Id, prefix, scopes.Count == 0 ? "(all of the owner's)" : string.Join(' ', scopes), record.ExpiresAt);
        return new CreatedApiKeyDto(ToDto(record), key);
    }

    public async Task<Result> RevokeAsync(Guid userId, Guid keyId, CancellationToken cancellationToken)
    {
        var key = await keys.FindAsync(keyId, userId, cancellationToken);
        if (key is null) return Error.NotFound("API key", keyId);
        if (key.RevokedAt is not null) return Result.Success();

        await keys.RevokeAsync(key.Id, clock.GetUtcNow(), cancellationToken);
        LogApiKeyRevoked(logger, userId, key.Id, key.Prefix);
        return Result.Success();
    }

    public async Task<ApiKeyIdentity?> ValidateAsync(string key, CancellationToken cancellationToken)
    {
        if (!key.StartsWith(Marker, StringComparison.Ordinal) || key.Length > MaxPresentedKeyLength) return null;

        var match = await keys.FindByHashAsync(SecureTokens.Hash(key), cancellationToken);
        var now = clock.GetUtcNow();
        if (match is null || match.RevokedAt is not null || match.ExpiresAt <= now) return null;

        // Coarse "last used", so authentication doesn't write on every request.
        if (match.LastUsedAt is null || now - match.LastUsedAt > LastUsedResolution)
        {
            await keys.MarkUsedAsync(match.Id, now, cancellationToken);
        }

        return new ApiKeyIdentity(match.Id, match.UserId, match.Scopes);
    }

    private static ApiKeyDto ToDto(ApiKeyRecord k) =>
        new(k.Id, k.Name, k.Prefix, k.Scopes, k.CreatedAt, k.ExpiresAt, k.LastUsedAt, k.RevokedAt);

    [LoggerMessage(EventId = LogEvents.ApiKeyCreated, Level = LogLevel.Information,
        Message = "User {UserId} created API key {ApiKeyId} ({ApiKeyPrefix}) with scopes {Scopes}, expiring {ExpiresAt}")]
    private static partial void LogApiKeyCreated(ILogger logger, Guid userId, Guid apiKeyId, string apiKeyPrefix, string scopes, DateTimeOffset expiresAt);

    [LoggerMessage(EventId = LogEvents.ApiKeyRevoked, Level = LogLevel.Information,
        Message = "User {UserId} revoked API key {ApiKeyId} ({ApiKeyPrefix})")]
    private static partial void LogApiKeyRevoked(ILogger logger, Guid userId, Guid apiKeyId, string apiKeyPrefix);
}
