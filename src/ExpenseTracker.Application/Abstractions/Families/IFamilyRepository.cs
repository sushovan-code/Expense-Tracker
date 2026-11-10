using ExpenseTracker.Domain.Families;

namespace ExpenseTracker.Application.Abstractions.Families;

/// <summary>Write-side access to families, members and invites.</summary>
public interface IFamilyRepository
{
    Task<Family?> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken);
    Task<FamilyMember?> GetMemberAsync(Guid familyId, Guid userId, CancellationToken cancellationToken);
    Task<int> CountOwnersAsync(Guid familyId, CancellationToken cancellationToken);

    /// <summary>How many families the user is a Member or Viewer in (owning a family does not count).</summary>
    Task<int> CountNonOwnerMembershipsAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> IsMemberAsync(Guid familyId, Guid userId, CancellationToken cancellationToken);
    Task<FamilyInvite?> GetInviteAsync(Guid familyId, Guid inviteId, CancellationToken cancellationToken);
    Task<FamilyInvite?> FindInviteByHashAsync(string tokenHash, CancellationToken cancellationToken);
    void Add(Family family);
    void Add(FamilyMember member);
    void Add(FamilyInvite invite);
    void Remove(FamilyMember member);
}
