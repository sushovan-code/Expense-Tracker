namespace ExpenseTracker.Domain.Families;

/// <summary>What a person may do inside their family. Separate from the app-wide roles.</summary>
public enum FamilyRole
{
    Owner,
    Member,
    Viewer,
}
