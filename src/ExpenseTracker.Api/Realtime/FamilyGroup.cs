namespace ExpenseTracker.Api.Realtime;

/// <summary>SignalR group names, one group per family.</summary>
internal static class FamilyGroup
{
    /// <summary>A group nobody joins, used when the caller has no family.</summary>
    public const string None = "family:none";

    public static string Name(Guid familyId) => $"family:{familyId}";
}
