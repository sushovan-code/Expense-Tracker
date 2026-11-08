using ExpenseTracker.Domain.Categories;

namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Write-side access to categories.</summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task<int> CountExpensesAsync(int categoryId, CancellationToken ct);
    void Add(Category category);
    void Remove(Category category);
}
