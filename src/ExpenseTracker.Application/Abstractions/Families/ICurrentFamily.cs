using ExpenseTracker.Domain.Families;

namespace ExpenseTracker.Application.Abstractions.Families;

/// <summary>
/// Who is calling and which family they belong to. Filled once per request after the existing authentication
/// and authorisation; it never touches the app-wide roles and permissions.
/// </summary>
public interface ICurrentFamily
{
    /// <summary>The signed-in user; <see cref="Guid.Empty"/> when nobody is signed in.</summary>
    Guid UserId { get; }

    /// <summary>The user's family, or null when they have none.</summary>
    Guid? FamilyId { get; }

    FamilyRole? Role { get; }
}
