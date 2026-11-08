using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// Effective access per user (active flag, security stamp, roles, permissions), cached with HybridCache
/// (in-memory now; add a distributed L2 such as Redis when running several API instances). Every
/// identity change invalidates the entry, so the cache only saves work and never serves stale grants.
/// </summary>
internal sealed class UserAccessProvider(AppDbContext dbContext, HybridCache cache) : IUserAccessProvider
{
    private const string Tag = "user-access";

    /// <summary>Bump when the cached <see cref="UserAccess"/> shape changes.</summary>
    private const int KeyVersion = 1;

    /// <summary>A safety net only: every identity change invalidates the entry straight away.</summary>
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(10);

    private static readonly HybridCacheEntryOptions EntryOptions = new()
    {
        Expiration = CacheLifetime,
        LocalCacheExpiration = CacheLifetime,
    };

    private static readonly IReadOnlyList<string> AllPermissions = Permissions.All.Select(p => p.Name).ToArray();

    public async Task<UserAccess?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        await cache.GetOrCreateAsync(
            Key(userId),
            (dbContext, userId),
            static (state, token) => new ValueTask<UserAccess?>(LoadAsync(state.dbContext, state.userId, token)),
            EntryOptions,
            [Tag],
            cancellationToken);

    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken) => await cache.RemoveAsync(Key(userId), cancellationToken);

    public async Task InvalidateAllAsync(CancellationToken cancellationToken) => await cache.RemoveByTagAsync(Tag, cancellationToken);

    private static string Key(Guid userId) => $"expensetracker:{Tag}:{userId:N}:v{KeyVersion}";

    private static async Task<UserAccess?> LoadAsync(AppDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.DisplayName, u.SecurityStamp, u.IsActive })
            .SingleOrDefaultAsync(cancellationToken);
        if (user is null) return null;

        var roles = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.Name!).ToListAsync(cancellationToken);

        IReadOnlyList<string> permissions;
        if (roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            // Enforced in code, not data: SuperAdmin can't be locked out of a permission by a bad edit.
            permissions = AllPermissions;
        }
        else
        {
            var fromRoles = await (
                from userRole in dbContext.UserRoles
                join claim in dbContext.RoleClaims on userRole.RoleId equals claim.RoleId
                where userRole.UserId == userId && claim.ClaimType == AppClaimTypes.Permission
                select claim.ClaimValue!).ToListAsync(cancellationToken);

            var direct = await dbContext.UserClaims
                .Where(c => c.UserId == userId && c.ClaimType == AppClaimTypes.Permission)
                .Select(c => c.ClaimValue!)
                .ToListAsync(cancellationToken);

            permissions = fromRoles.Concat(direct).Where(Permissions.IsKnown).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        }

        return new UserAccess(
            userId,
            user.Email ?? "",
            user.DisplayName,
            user.SecurityStamp ?? "",
            user.IsActive,
            roles.OrderByDescending(AppRoles.RankOf).ToArray(),
            permissions);
    }
}
