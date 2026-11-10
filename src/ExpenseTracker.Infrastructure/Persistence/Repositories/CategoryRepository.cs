using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository(AppDbContext dbContext) : ICategoryRepository
{
    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken) =>
        dbContext.Categories.AnyAsync(c => c.Name == name && c.Id != excludeId, cancellationToken);

    public Task<int> CountExpensesAsync(Guid categoryId, CancellationToken cancellationToken) =>
        // Counts every expense in the family, including other members' private ones, so a category in use can't be deleted.
        dbContext.Expenses.IgnoreQueryFilters([AppDbContext.PrivateFilter]).CountAsync(e => e.CategoryId == categoryId, cancellationToken);

    public void Add(Category category) => dbContext.Categories.Add(category);

    public void Remove(Category category) => dbContext.Categories.Remove(category);
}
