using ExpenseTracker.Domain.Categories;

namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Write-side access to categories.</summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken cancellationToken);
    Task<int> CountExpensesAsync(int categoryId, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
}
