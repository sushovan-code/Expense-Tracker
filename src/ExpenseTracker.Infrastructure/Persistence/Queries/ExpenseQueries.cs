using System.Linq.Expressions;
using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Queries;

internal sealed class ExpenseQueries(AppDbContext db) : IExpenseQueries
{
    /// <summary>Projection translated to SQL, so only the needed columns are read.</summary>
    private static readonly Expression<Func<Expense, ExpenseDto>> ToDto = e => new ExpenseDto(
        e.Id, e.Description, e.Amount, e.Date, e.CategoryId, e.Category!.Name, e.Category.Color, e.Notes, e.CreatedAt);

    public async Task<ExpensePage> ListAsync(ExpenseFilter filter, PageRequest request, SortSpec sort, CancellationToken ct)
    {
        var filtered = Filter(filter);

        var totalCount = await filtered.CountAsync(ct);
        var totalAmount = totalCount == 0 ? 0 : await filtered.SumAsync(e => e.Amount, ct);
        var (page, pageSize) = Paging.Resolve(request, totalCount);

        var items = await Sort(filtered, sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync(ct);

        return new ExpensePage(items, page, pageSize, totalCount, totalAmount);
    }

    public async Task<IReadOnlyList<ExpenseDto>> ListAllAsync(ExpenseFilter filter, SortSpec sort, int maxRows, CancellationToken ct) =>
        await Sort(Filter(filter), sort).Take(maxRows).Select(ToDto).ToListAsync(ct);

    public Task<ExpenseDto?> GetAsync(int id, CancellationToken ct) =>
        db.Expenses.AsNoTracking().Where(e => e.Id == id).Select(ToDto).FirstOrDefaultAsync(ct);

    public async Task<ExpenseSummary> SummarizeAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var expenses = Filter(new ExpenseFilter(from, to, null, null));

        var byCategory = await expenses
            .GroupBy(e => new { e.CategoryId, e.Category!.Name, e.Category.Color })
            .Select(g => new CategoryTotal(g.Key.CategoryId, g.Key.Name, g.Key.Color, g.Sum(e => e.Amount), g.Count()))
            .ToListAsync(ct);

        var byMonth = await expenses
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new MonthTotal(g.Key.Year, g.Key.Month, g.Sum(e => e.Amount)))
            .ToListAsync(ct);

        var total = byCategory.Sum(c => c.Total);
        var count = byCategory.Sum(c => c.Count);

        return new ExpenseSummary(
            total,
            count,
            count == 0 ? 0 : Math.Round(total / count, 2),
            [.. byCategory.OrderByDescending(c => c.Total)],
            [.. byMonth.OrderBy(m => m.Year).ThenBy(m => m.Month)]);
    }

    private IQueryable<Expense> Filter(ExpenseFilter f)
    {
        var query = db.Expenses.AsNoTracking();
        if (f.From is not null) query = query.Where(e => e.Date >= f.From);
        if (f.To is not null) query = query.Where(e => e.Date <= f.To);
        if (f.CategoryId is not null) query = query.Where(e => e.CategoryId == f.CategoryId);
        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var term = f.Search.Trim();
            query = query.Where(e => e.Description.Contains(term) || (e.Notes != null && e.Notes.Contains(term)));
        }
        return query;
    }

    /// <summary>Sorts by an allow-listed field, with Id as a tie-breaker so paging is stable.</summary>
    private static IQueryable<Expense> Sort(IQueryable<Expense> query, SortSpec sort) => sort.Field switch
    {
        "amount" => OrderBy(query, e => e.Amount, sort.Descending),
        "description" => OrderBy(query, e => e.Description, sort.Descending),
        "category" => OrderBy(query, e => e.Category!.Name, sort.Descending),
        "createdat" => OrderBy(query, e => e.CreatedAt, sort.Descending),
        _ => OrderBy(query, e => e.Date, sort.Descending),
    };

    private static IQueryable<Expense> OrderBy<TKey>(IQueryable<Expense> query, Expression<Func<Expense, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key).ThenByDescending(e => e.Id) : query.OrderBy(key).ThenBy(e => e.Id);
}
