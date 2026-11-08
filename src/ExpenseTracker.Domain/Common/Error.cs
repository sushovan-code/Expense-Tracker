namespace ExpenseTracker.Domain.Common;

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
