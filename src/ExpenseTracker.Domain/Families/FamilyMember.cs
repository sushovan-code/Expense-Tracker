using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Families;

/// <summary>A user's place in a family. A user belongs to at most one family (unique UserId).</summary>
public sealed class FamilyMember : Entity
{
    private FamilyMember() { } // EF Core

    public Guid FamilyId { get; private set; }
    public Guid UserId { get; private set; }
    public FamilyRole Role { get; private set; }
    public DateTimeOffset JoinedAt { get; private set; }

    public static FamilyMember Create(Guid familyId, Guid userId, FamilyRole role, DateTimeOffset joinedAt) =>
        new() { FamilyId = familyId, UserId = userId, Role = role, JoinedAt = joinedAt };

    public void ChangeRole(FamilyRole role) => Role = role;
}
