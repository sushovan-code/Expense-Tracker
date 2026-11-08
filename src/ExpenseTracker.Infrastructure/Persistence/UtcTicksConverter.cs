using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>
/// Stores a <see cref="DateTimeOffset"/> as its UTC ticks (a 64-bit integer). SQLite can then compare and
/// sort it in SQL, which it can't do for text-stored offsets. Values are read back as UTC.
/// </summary>
internal sealed class UtcTicksConverter() : ValueConverter<DateTimeOffset, long>(
    value => value.UtcTicks,
    ticks => new DateTimeOffset(ticks, TimeSpan.Zero));
