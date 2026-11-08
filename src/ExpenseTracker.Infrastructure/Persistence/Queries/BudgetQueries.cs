using ExpenseTracker.Application.Abstractions.Queries;
using ExpenseTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Queries;

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
