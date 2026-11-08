using System.Linq.Expressions;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Categories;
using ExpenseTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Queries;

internal sealed class CategoryQueries(AppDbContext db) : ICategoryQueries
{
    public async Task<PagedResult<CategoryDto>> ListAsync(string? search, PageRequest request, SortSpec sort, CancellationToken ct)
    {
        var categories = db.Categories.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            categories = categories.Where(c => c.Name.Contains(search));
        }

        var totalCount = await categories.CountAsync(ct);
        var (page, pageSize) = Paging.Resolve(request, totalCount);

        var items = await Sort(categories, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Color, c.MonthlyBudget, c.Expenses.Count))
            .ToListAsync(ct);

        return new PagedResult<CategoryDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<CategoryLookupDto>> LookupAsync(CancellationToken ct) =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryLookupDto(c.Id, c.Name, c.Color))
            .ToListAsync(ct);

    private static IQueryable<Category> Sort(IQueryable<Category> query, SortSpec sort) => sort.Field switch
    {
        "expenses" => OrderBy(query, c => c.Expenses.Count, sort.Descending),
        "budget" => OrderBy(query, c => c.MonthlyBudget, sort.Descending),
        _ => OrderBy(query, c => c.Name, sort.Descending),
    };

    private static IQueryable<Category> OrderBy<TKey>(IQueryable<Category> query, Expression<Func<Category, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key).ThenBy(c => c.Id) : query.OrderBy(key).ThenBy(c => c.Id);
}

internal sealed class BudgetQueries(AppDbContext db) : IBudgetQueries
{
    public async Task<IReadOnlyList<CategorySpending>> GetBudgetedSpendingAsync(DateOnly from, DateOnly to, CancellationToken ct) =>
        // Sort before projecting: EF can't translate ordering on a constructed record.
        await Spending(db.Categories.Where(c => c.MonthlyBudget != null).OrderBy(c => c.Name), from, to)
            .ToListAsync(ct);

    public Task<CategorySpending?> GetCategorySpendingAsync(int categoryId, DateOnly from, DateOnly to, CancellationToken ct) =>
        Spending(db.Categories.Where(c => c.Id == categoryId), from, to).FirstOrDefaultAsync(ct);

    private static IQueryable<CategorySpending> Spending(IQueryable<Category> categories, DateOnly from, DateOnly to) =>
        categories.AsNoTracking().Select(c => new CategorySpending(
            c.Id,
            c.Name,
            c.Color,
            c.MonthlyBudget,
            c.Expenses.Where(e => e.Date >= from && e.Date <= to).Sum(e => (decimal?)e.Amount) ?? 0));
}
