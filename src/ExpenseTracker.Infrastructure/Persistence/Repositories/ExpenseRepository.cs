using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(AppDbContext db) : IExpenseRepository
{
    public Task<Expense?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);

    public void Add(Expense expense) => db.Expenses.Add(expense);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct) =>
        await db.Expenses.Where(e => e.Id == id).ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyList<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var existing = await db.Expenses.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(ct);
        if (existing.Count > 0)
        {
            await db.Expenses.Where(e => existing.Contains(e.Id)).ExecuteDeleteAsync(ct);
        }
        return existing;
    }
}
