namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>Stored personal API keys (hashes only).</summary>
public interface IApiKeyStore
{
    /// <summary>The user's keys, newest first.</summary>
    Task<IReadOnlyList<ApiKeyRecord>> ListAsync(Guid userId, CancellationToken cancellationToken);

    Task<int> CountActiveAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
    Task AddAsync(ApiKeyRecord key, CancellationToken cancellationToken);

    /// <summary>The key, only if it belongs to <paramref name="userId"/>.</summary>
    Task<ApiKeyRecord?> FindAsync(Guid keyId, Guid userId, CancellationToken cancellationToken);

    Task<ApiKeyRecord?> FindByHashAsync(string keyHash, CancellationToken cancellationToken);
    Task RevokeAsync(Guid keyId, DateTimeOffset now, CancellationToken cancellationToken);
    Task MarkUsedAsync(Guid keyId, DateTimeOffset now, CancellationToken cancellationToken);
}
