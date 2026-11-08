using ExpenseTracker.Domain.Categories;

namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Write-side access to categories.</summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);
    Task<int> CountExpensesAsync(Guid categoryId, CancellationToken cancellationToken);
    void Add(Category category);
    void Remove(Category category);
}
