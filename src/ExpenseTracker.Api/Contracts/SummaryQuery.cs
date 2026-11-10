namespace ExpenseTracker.Api.Contracts;

public sealed record SummaryQuery(DateOnly? From, DateOnly? To, Guid? MemberId = null);
