using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>User lists for administration, projected straight from the identity tables.</summary>
internal sealed class UserQueries(AppDbContext dbContext, TimeProvider clock) : IUserQueries
{
    public async Task<PagedResult<UserSummaryDto>> ListAsync(UserListRequest request, SortSpec sort, CancellationToken cancellationToken)
    {
        var query = FilterUsers(request);
        var total = await query.CountAsync(cancellationToken);
        var (page, pageSize) = Paging.Resolve(request.Page, total);

        var rows = await SortUsers(query, sort).ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { u.Id, u.Email, u.DisplayName, u.IsActive, u.LockoutEnd, u.CreatedAt, u.LastLoginAt })
            .ToListAsync(cancellationToken);

        var roles = await RolesByUserAsync(rows.Select(r => r.Id).ToList(), cancellationToken);
        var now = clock.GetUtcNow();
        var items = rows.Select(r => new UserSummaryDto(
            r.Id, r.Email ?? "", r.DisplayName, roles.GetValueOrDefault(r.Id) ?? [], r.IsActive, r.LockoutEnd > now, r.CreatedAt, r.LastLoginAt))
            .ToList();

        return new PagedResult<UserSummaryDto>(items, page, pageSize, total);
    }

    private IQueryable<AppUser> FilterUsers(UserListRequest request)
    {
        IQueryable<AppUser> query = dbContext.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var pattern = LikePatterns.Contains(request.Search.Trim());
            query = query.Where(u => EF.Functions.Like(u.Email!, pattern, LikePatterns.EscapeCharacter)
                || EF.Functions.Like(u.DisplayName, pattern, LikePatterns.EscapeCharacter));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = request.Role.Trim().ToUpperInvariant();
            query = query.Where(u => dbContext.UserRoles.Any(ur => ur.UserId == u.Id && dbContext.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == role)));
        }

        return query;
    }

    private static IOrderedQueryable<AppUser> SortUsers(IQueryable<AppUser> query, SortSpec sort) => (sort.Field, sort.Descending) switch
    {
        ("email", false) => query.OrderBy(u => u.Email),
        ("email", true) => query.OrderByDescending(u => u.Email),
        ("created", false) => query.OrderBy(u => u.CreatedAt),
        ("created", true) => query.OrderByDescending(u => u.CreatedAt),
        ("lastlogin", false) => query.OrderBy(u => u.LastLoginAt),
        ("lastlogin", true) => query.OrderByDescending(u => u.LastLoginAt),
        (_, true) => query.OrderByDescending(u => u.DisplayName),
        _ => query.OrderBy(u => u.DisplayName),
    };

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> RolesByUserAsync(List<Guid> userIds, CancellationToken cancellationToken)
    {
        var pairs = await (
            from userRole in dbContext.UserRoles
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, role.Name }).ToListAsync(cancellationToken);

        return pairs.GroupBy(p => p.UserId).ToDictionary(
            g => g.Key,
            g => (IReadOnlyList<string>)g.Select(p => p.Name!).OrderByDescending(AppRoles.RankOf).ToList());
    }
}
