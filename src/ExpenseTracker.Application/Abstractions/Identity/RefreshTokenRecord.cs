namespace ExpenseTracker.Application.Abstractions.Identity;

/// <summary>
/// One link in a rotating refresh-token chain; only the token's hash is kept. Every token of one sign-in shares
/// <see cref="SessionId"/>, so presenting an already-rotated token can revoke the whole chain.
/// <see cref="ReplacedById"/> is set when the token was rotated (as opposed to revoked by sign-out).
/// </summary>
public sealed record RefreshTokenRecord(
    Guid Id,
    Guid UserId,
    Guid SessionId,
    string TokenHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    Guid? ReplacedById,
    string? CreatedByIp);
