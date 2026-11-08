namespace ExpenseTracker.Api.Realtime;

/// <param name="Action">created | updated | deleted</param>
/// <param name="OriginClientId">The browser tab that caused the change (X-Client-Id), so it can skip its own echo.</param>
public sealed record ChangeNotice(string Action, IReadOnlyCollection<Guid> Ids, string? OriginClientId);
