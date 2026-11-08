namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// One link in a rotating refresh-token chain. Only a SHA-256 hash is stored. Every token of one sign-in
/// shares a <see cref="SessionId"/>, so presenting an already-rotated token can revoke the whole chain.
/// </summary>
public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid SessionId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    /// <summary>Set when the token was rotated (as opposed to revoked by sign-out).</summary>
    public Guid? ReplacedById { get; set; }
    public string? CreatedByIp { get; set; }
}
