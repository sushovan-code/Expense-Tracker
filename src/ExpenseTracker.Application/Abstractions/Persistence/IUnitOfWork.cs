namespace ExpenseTracker.Application.Abstractions.Persistence;

/// <summary>Commits the changes tracked by the repositories as one unit.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}
