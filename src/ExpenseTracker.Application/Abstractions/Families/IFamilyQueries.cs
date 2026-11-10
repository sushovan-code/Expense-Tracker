using ExpenseTracker.Application.Features.Families;

namespace ExpenseTracker.Application.Abstractions.Families;

public interface IFamilyQueries
{
    /// <summary>Every family the user belongs to, earliest joined first.</summary>
    Task<IReadOnlyList<FamilySummaryDto>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>The family with its members and its open (unused, unrevoked, unexpired) invites.</summary>
    Task<FamilyDto?> GetAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken);
}
