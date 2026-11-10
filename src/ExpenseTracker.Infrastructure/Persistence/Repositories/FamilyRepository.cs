using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

// Families, members and invites have no family query filter: they define the family boundary, and every
// lookup here is already scoped by a family id or an invite token.
internal sealed class FamilyRepository(AppDbContext dbContext) : IFamilyRepository
{
    public Task<Family?> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken) =>
        dbContext.Families.FirstOrDefaultAsync(f => f.Id == familyId, cancellationToken);

    public Task<FamilyMember?> GetMemberAsync(Guid familyId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.FamilyMembers.FirstOrDefaultAsync(m => m.FamilyId == familyId && m.UserId == userId, cancellationToken);

    public Task<int> CountOwnersAsync(Guid familyId, CancellationToken cancellationToken) =>
        dbContext.FamilyMembers.CountAsync(m => m.FamilyId == familyId && m.Role == FamilyRole.Owner, cancellationToken);

    public Task<int> CountNonOwnerMembershipsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.FamilyMembers.CountAsync(m => m.UserId == userId && m.Role != FamilyRole.Owner, cancellationToken);

    public Task<bool> IsMemberAsync(Guid familyId, Guid userId, CancellationToken cancellationToken) =>
        dbContext.FamilyMembers.AnyAsync(m => m.FamilyId == familyId && m.UserId == userId, cancellationToken);

    public Task<FamilyInvite?> GetInviteAsync(Guid familyId, Guid inviteId, CancellationToken cancellationToken) =>
        dbContext.FamilyInvites.FirstOrDefaultAsync(i => i.FamilyId == familyId && i.Id == inviteId, cancellationToken);

    public Task<FamilyInvite?> FindInviteByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.FamilyInvites.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, cancellationToken);

    public void Add(Family family) => dbContext.Families.Add(family);

    public void Add(FamilyMember member) => dbContext.FamilyMembers.Add(member);

    public void Add(FamilyInvite invite) => dbContext.FamilyInvites.Add(invite);

    public void Remove(FamilyMember member) => dbContext.FamilyMembers.Remove(member);
}
