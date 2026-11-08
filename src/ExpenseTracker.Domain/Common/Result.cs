using System.Diagnostics.CodeAnalysis;

namespace ExpenseTracker.Domain.Common;

/// <summary>Outcome of an operation that returns no value.</summary>
public sealed class Result
{
    private Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static implicit operator Result(Error error) => new(error);
}
