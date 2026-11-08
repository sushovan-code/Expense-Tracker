namespace ExpenseTracker.Domain.Common;

/// <summary>
/// Base type for entities. Identity is a time-ordered GUID (v7) assigned when the entity is created, not by
/// the database, so an entity has its ID before it's saved and any store can hold it (see ADR 0001).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();
}
