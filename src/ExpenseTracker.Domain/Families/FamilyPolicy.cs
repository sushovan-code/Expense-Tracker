namespace ExpenseTracker.Domain.Families;

/// <summary>
/// What each family role may do. Pure functions, independent of the app-wide <c>AccessPolicy</c>: the app
/// permission decides whether you may use expenses at all, this decides what you may do inside your family.
/// </summary>
public static class FamilyPolicy
{
    /// <summary>Members, invites, roles, family name, categories and budgets.</summary>
    public static bool CanManageFamily(FamilyRole role) => role == FamilyRole.Owner;

    public static bool CanAddExpense(FamilyRole role) => role is FamilyRole.Owner or FamilyRole.Member;

    /// <summary>Owners change any shared expense; members only their own; viewers none.</summary>
    public static bool CanChangeExpense(FamilyRole role, bool isOwnExpense) => role switch
    {
        FamilyRole.Owner => true,
        FamilyRole.Member => isOwnExpense,
        _ => false,
    };

    /// <summary>True when removing or demoting this member would leave the family without an owner.</summary>
    public static bool WouldLeaveNoOwner(FamilyRole currentRole, FamilyRole? newRole, int ownerCount) =>
        currentRole == FamilyRole.Owner && newRole != FamilyRole.Owner && ownerCount <= 1;
}
