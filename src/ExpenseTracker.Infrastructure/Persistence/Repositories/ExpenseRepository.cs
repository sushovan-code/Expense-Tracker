using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseRepository(AppDbContext dbContext) : IExpenseRepository
{
    public Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public void Add(Expense expense) => dbContext.Expenses.Add(expense);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken) =>
        await dbContext.Expenses.Where(e => e.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<IReadOnlyList<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Expenses.Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            await dbContext.Expenses.Where(e => existing.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken);
        }
        return existing;
    }
}
