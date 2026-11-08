using System.Diagnostics.CodeAnalysis;

namespace ExpenseTracker.Domain.Common;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict,
    /// <summary>The caller could not be authenticated (bad credentials, expired or revoked session).</summary>
    Unauthorized,
    /// <summary>The caller is authenticated but not allowed to do this.</summary>
    Forbidden,
}

/// <summary>An expected failure (a broken rule, a missing entity). Unexpected failures are exceptions.</summary>
/// <param name="Field">For validation errors: the input field (camelCase) the error belongs to.</param>
public sealed record Error(ErrorKind Kind, string Title, string Detail, string? Field = null)
{
    public static Error NotFound(string resource, int id) =>
        new(ErrorKind.NotFound, $"{resource} not found", $"{resource} with id {id} does not exist.");

    public static Error NotFound(string resource, Guid id) =>
        new(ErrorKind.NotFound, $"{resource} not found", $"{resource} with id {id} does not exist.");

    public static Error Unauthorized(string title, string detail) =>
        new(ErrorKind.Unauthorized, title, detail);

    public static Error Forbidden(string detail) =>
        new(ErrorKind.Forbidden, "Forbidden", detail);

    public static Error Validation(string field, string detail) =>
        new(ErrorKind.Validation, "One or more validation errors occurred.", detail, field);

    public static Error Conflict(string title, string detail) =>
        new(ErrorKind.Conflict, title, detail);
}

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
