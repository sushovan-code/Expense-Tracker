using ExpenseTracker.Application.Abstractions.Families;
using ExpenseTracker.Domain.Families;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// The caller and their family for the current request (scoped). The API fills it in once after authentication;
/// the database context reads it for its query filters. It has no dependencies, so the context can use it.
/// </summary>
public sealed class FamilyContext : ICurrentFamily
{
    public Guid UserId { get; private set; }
    public Guid? FamilyId { get; private set; }
    public FamilyRole? Role { get; private set; }

    public void Set(Guid userId, Guid? familyId, FamilyRole? role)
    {
        UserId = userId;
        FamilyId = familyId;
        Role = role;
    }
}
