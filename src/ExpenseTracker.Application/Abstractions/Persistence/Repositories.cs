using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Write-side access to expenses (aggregate loading and persistence).</summary>
public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(int id, CancellationToken ct);
    void Add(Expense expense);

    /// <summary>Deletes immediately (set-based); returns false when the expense didn't exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    /// <summary>Deletes the given ids that exist; returns the ids actually deleted.</summary>
    Task<IReadOnlyList<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken ct);
}

/// <summary>Write-side access to categories.</summary>
public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? excludeId, CancellationToken ct);
    Task<int> CountExpensesAsync(int categoryId, CancellationToken ct);
    void Add(Category category);
    void Remove(Category category);
}

/// <summary>Commits the changes tracked by the repositories as one unit.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
