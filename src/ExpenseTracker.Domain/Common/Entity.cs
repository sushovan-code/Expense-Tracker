namespace ExpenseTracker.Domain.Common;

/// <summary>Base type for entities: identity is the database-generated <see cref="Id"/>.</summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}
