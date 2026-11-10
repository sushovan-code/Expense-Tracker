using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Write-side access to expenses (aggregate loading and persistence).</summary>
public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Expense expense);

    /// <summary>Deletes immediately (set-based); returns false when the expense didn't exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the given ids that exist (and, when <paramref name="onlyCreatedBy"/> is set, were created by that user);
    /// returns the ids actually deleted.
    /// </summary>
    Task<IReadOnlyList<Guid>> DeleteManyAsync(IReadOnlyCollection<Guid> ids, Guid? onlyCreatedBy, CancellationToken cancellationToken);
}
