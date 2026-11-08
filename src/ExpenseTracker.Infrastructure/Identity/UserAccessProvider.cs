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
internal sealed class UserAccessProvider(AppDbContext db, HybridCache cache) : IUserAccessProvider
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

    public async Task<UserAccess?> GetAsync(Guid userId, CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            Key(userId),
            (db, userId),
            static (state, token) => new ValueTask<UserAccess?>(LoadAsync(state.db, state.userId, token)),
            EntryOptions,
            [Tag],
            ct);

    public async Task InvalidateAsync(Guid userId, CancellationToken ct) => await cache.RemoveAsync(Key(userId), ct);

    public async Task InvalidateAllAsync(CancellationToken ct) => await cache.RemoveByTagAsync(Tag, ct);

    private static string Key(Guid userId) => $"expensetracker:{Tag}:{userId:N}:v{KeyVersion}";

    private static async Task<UserAccess?> LoadAsync(AppDbContext db, Guid userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Email, u.DisplayName, u.SecurityStamp, u.IsActive })
            .SingleOrDefaultAsync(ct);
        if (user is null) return null;

        var roles = await (
            from userRole in db.UserRoles
            join role in db.Roles on userRole.RoleId equals role.Id
            where userRole.UserId == userId
            select role.Name!).ToListAsync(ct);

        IReadOnlyList<string> permissions;
        if (roles.Contains(AppRoles.SuperAdmin, StringComparer.OrdinalIgnoreCase))
        {
            // Enforced in code, not data: SuperAdmin can't be locked out of a permission by a bad edit.
            permissions = AllPermissions;
        }
        else
        {
            var fromRoles = await (
                from userRole in db.UserRoles
                join claim in db.RoleClaims on userRole.RoleId equals claim.RoleId
                where userRole.UserId == userId && claim.ClaimType == AppClaimTypes.Permission
                select claim.ClaimValue!).ToListAsync(ct);

            var direct = await db.UserClaims
                .Where(c => c.UserId == userId && c.ClaimType == AppClaimTypes.Permission)
                .Select(c => c.ClaimValue!)
                .ToListAsync(ct);

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
