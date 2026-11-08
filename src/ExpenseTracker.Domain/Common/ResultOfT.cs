using System.Diagnostics.CodeAnalysis;

namespace ExpenseTracker.Domain.Common;

/// <summary>Outcome of an operation that returns a value on success.</summary>
public sealed class Result<T>
{
    private Result(T value) => Value = value;
    private Result(Error error) => Error = error;

    public T? Value { get; }
    public Error? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    /// <summary>Explicit success factory (needed when T is an interface, which implicit conversions can't take).</summary>
    public static Result<T> Success(T value) => new(value);

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Error error) => new(error);
}
