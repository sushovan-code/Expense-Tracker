using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(AppDbContext db) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct) =>
        db.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, ct);

    public Task<int> CountExpensesAsync(int categoryId, CancellationToken ct) =>
        db.Expenses.CountAsync(e => e.CategoryId == categoryId, ct);

    public void Add(Category category) => db.Categories.Add(category);

    public void Remove(Category category) => db.Categories.Remove(category);
}
