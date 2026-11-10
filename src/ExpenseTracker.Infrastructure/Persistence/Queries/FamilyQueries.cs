using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Application.Features.Families;
using ExpenseTracker.Domain.Families;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Queries;

internal sealed class FamilyQueries(AppDbContext dbContext) : IFamilyQueries
{
    public Task<FamilyMembership?> FindMembershipAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.FamilyMembers.AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new FamilyMembership(m.FamilyId, m.Role))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<FamilyDto?> GetAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var family = await dbContext.Families.AsNoTracking().FirstOrDefaultAsync(f => f.Id == familyId, cancellationToken);
        if (family is null) return null;

        var members = await dbContext.FamilyMembers.AsNoTracking()
            .Where(m => m.FamilyId == familyId)
            .Join(dbContext.Users, m => m.UserId, u => u.Id,
                (m, u) => new FamilyMemberDto(m.UserId, u.DisplayName, u.Email ?? "", m.Role, m.JoinedAt))
            .ToListAsync(cancellationToken);

        var invites = await dbContext.FamilyInvites.AsNoTracking()
            .Where(i => i.FamilyId == familyId && i.AcceptedAt == null && i.RevokedAt == null && i.ExpiresAt > now)
            .OrderBy(i => i.CreatedAt)
            .Select(i => new InviteDto(i.Id, i.Role, i.CreatedAt, i.ExpiresAt))
            .ToListAsync(cancellationToken);

        return new FamilyDto(family.Id, family.Name, FamilyRole.Viewer, [.. members.OrderBy(m => m.Role).ThenBy(m => m.DisplayName)], invites);
    }
}
