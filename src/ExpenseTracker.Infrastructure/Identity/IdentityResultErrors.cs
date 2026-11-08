using ExpenseTracker.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Maps failed <see cref="IdentityResult"/>s onto field-level validation errors the client can show inline.</summary>
internal static class IdentityResultErrors
{
    public static Error ToError(this IdentityResult result, string passwordField = "password") =>
        ToError(result.Errors.ToList(), passwordField);

    public static Error ToError(IReadOnlyList<IdentityError> errors, string passwordField = "password")
    {
        if (errors.Count == 0)
            return new Error(ErrorKind.Validation, "One or more validation errors occurred.", "The request could not be completed.");

        var field = FieldFor(errors[0].Code, passwordField);
        var detail = string.Join(" ", errors
            .Where(e => FieldFor(e.Code, passwordField) == field)
            .Select(e => e.Description)
            .Distinct());

        return field is null
            ? new Error(ErrorKind.Validation, "One or more validation errors occurred.", detail)
            : Error.Validation(field, detail);
    }

    private static string? FieldFor(string code, string passwordField) => code switch
    {
        "PasswordMismatch" => "currentPassword",
        _ when code.StartsWith("Password", StringComparison.Ordinal) => passwordField,
        _ when code.Contains("Email", StringComparison.Ordinal) || code.Contains("UserName", StringComparison.Ordinal) => "email",
        _ when code.Contains("Role", StringComparison.Ordinal) => "roles",
        _ => null,
    };
}
