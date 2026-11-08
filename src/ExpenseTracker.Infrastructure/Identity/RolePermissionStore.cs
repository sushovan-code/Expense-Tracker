using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Role permissions are stored as Identity role claims of type <c>permission</c>.</summary>
internal sealed class RolePermissionStore(AppDbContext dbContext) : IRolePermissionStore
{
    public async Task<IReadOnlyList<RolePermissions>> ListAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Roles.AsNoTracking().Select(r => new { r.Id, r.Name }).ToListAsync(cancellationToken);
        var claims = await dbContext.RoleClaims.AsNoTracking()
            .Where(c => c.ClaimType == AppClaimTypes.Permission)
            .Select(c => new { c.RoleId, c.ClaimValue })
            .ToListAsync(cancellationToken);
        var counts = await dbContext.UserRoles.GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles.Select(r => new RolePermissions(
                r.Name!,
                claims.Where(c => c.RoleId == r.Id).Select(c => c.ClaimValue!).ToList(),
                counts.GetValueOrDefault(r.Id)))
            .ToList();
    }

    public async Task<RolePermissions?> FindAsync(string role, CancellationToken cancellationToken)
    {
        var entity = await FindRoleAsync(role, cancellationToken);
        if (entity is null) return null;

        var permissions = await PermissionClaims(entity.Id).Select(c => c.ClaimValue!).ToListAsync(cancellationToken);
        var userCount = await dbContext.UserRoles.CountAsync(ur => ur.RoleId == entity.Id, cancellationToken);
        return new RolePermissions(entity.Name!, permissions, userCount);
    }

    public async Task UpdateAsync(string role, IReadOnlyCollection<string> added, IReadOnlyCollection<string> removed, CancellationToken cancellationToken)
    {
        var entity = await FindRoleAsync(role, cancellationToken)
            ?? throw new InvalidOperationException($"Role '{role}' does not exist.");

        var toRemove = await PermissionClaims(entity.Id).Where(c => removed.Contains(c.ClaimValue!)).ToListAsync(cancellationToken);
        dbContext.RoleClaims.RemoveRange(toRemove);
        dbContext.RoleClaims.AddRange(added.Select(p => new IdentityRoleClaim<Guid>
        {
            RoleId = entity.Id,
            ClaimType = AppClaimTypes.Permission,
            ClaimValue = p,
        }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<AppRole?> FindRoleAsync(string role, CancellationToken cancellationToken)
    {
        var normalizedName = role.ToUpperInvariant();
        return dbContext.Roles.SingleOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);
    }

    private IQueryable<IdentityRoleClaim<Guid>> PermissionClaims(Guid roleId) =>
        dbContext.RoleClaims.Where(c => c.RoleId == roleId && c.ClaimType == AppClaimTypes.Permission);
}
