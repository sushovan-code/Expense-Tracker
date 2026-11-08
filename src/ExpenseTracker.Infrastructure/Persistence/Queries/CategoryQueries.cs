using System.Linq.Expressions;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Queries;

internal sealed class CategoryQueries(AppDbContext dbContext) : ICategoryQueries
{
    public async Task<PagedResult<CategoryDto>> ListAsync(string? search, PageRequest request, SortSpec sort, CancellationToken cancellationToken)
    {
        var categories = dbContext.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = LikePatterns.Contains(search.Trim());
            categories = categories.Where(c => EF.Functions.Like(c.Name, pattern, LikePatterns.EscapeCharacter));
        }

        var totalCount = await categories.CountAsync(cancellationToken);
        var (page, pageSize) = Paging.Resolve(request, totalCount);

        var items = await Sort(categories, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Color, c.MonthlyBudget, c.Expenses.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<CategoryDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken cancellationToken) =>
        await dbContext.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryLookupDto(c.Id, c.Name, c.Color))
            .ToListAsync(cancellationToken);

    private static IQueryable<Category> Sort(IQueryable<Category> query, SortSpec sort) => sort.Field switch
    {
        "expenses" => OrderBy(query, c => c.Expenses.Count, sort.Descending),
        "budget" => OrderBy(query, c => c.MonthlyBudget, sort.Descending),
        _ => OrderBy(query, c => c.Name, sort.Descending),
    };

    private static IQueryable<Category> OrderBy<TKey>(IQueryable<Category> query, Expression<Func<Category, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key).ThenBy(c => c.Id) : query.OrderBy(key).ThenBy(c => c.Id);
}
