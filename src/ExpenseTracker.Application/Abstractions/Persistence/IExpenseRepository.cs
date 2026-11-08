using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Write-side access to expenses (aggregate loading and persistence).</summary>
public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(int id, CancellationToken cancellationToken);
    void Add(Expense expense);

    /// <summary>Deletes immediately (set-based); returns false when the expense didn't exist.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);

    /// <summary>Deletes the given ids that exist; returns the ids actually deleted.</summary>
    Task<IReadOnlyList<int>> DeleteManyAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken);
}
