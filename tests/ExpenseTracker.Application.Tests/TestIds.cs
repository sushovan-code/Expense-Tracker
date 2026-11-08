namespace ExpenseTracker.Application.Tests;

/// <summary>Readable, fixed IDs for tests: <c>TestIds.Of(7)</c> is always the same GUID.</summary>
internal static class TestIds
{
    public static Guid Of(int number) => new($"00000000-0000-0000-0000-{number:D12}");
}
