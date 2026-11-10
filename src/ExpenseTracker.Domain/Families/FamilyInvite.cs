using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Families;

/// <summary>A single-use invite link. Only the hash of its token is stored.</summary>
public sealed class FamilyInvite : Entity
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(48);

    private FamilyInvite() { } // EF Core

    public Guid FamilyId { get; private set; }
    public FamilyRole Role { get; private set; }
    public string TokenHash { get; private set; } = "";
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static FamilyInvite Create(Guid familyId, FamilyRole role, string tokenHash, Guid createdByUserId, DateTimeOffset now) =>
        new()
        {
            FamilyId = familyId,
            Role = role,
            TokenHash = tokenHash,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = now + Lifetime,
        };

    public bool IsUsable(DateTimeOffset now) => AcceptedAt is null && RevokedAt is null && now < ExpiresAt;

    /// <summary>Marks the invite used; fails if it is expired, revoked or already used.</summary>
    public Result Accept(DateTimeOffset now)
    {
        if (!IsUsable(now)) return FamilyErrors.InvalidInvite;
        AcceptedAt = now;
        return Result.Success();
    }

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}
