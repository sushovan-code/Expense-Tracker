using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

public sealed record CreateApiKeyRequest(
    [property: Required, StringLength(CredentialLimits.ApiKeyNameMaxLength, MinimumLength = 1), Description("Label to recognise the key by.")]
    string Name,
    [property: Description("Permissions to limit the key to (must be ones you hold). Omit for all of yours.")]
    string[]? Scopes,
    [property: Range(CredentialLimits.ApiKeyMinLifetimeDays, CredentialLimits.ApiKeyMaxLifetimeDays), Description("Days until the key expires (default 90).")]
    int ExpiresInDays = CredentialLimits.ApiKeyDefaultLifetimeDays)
{
    public CreateApiKeyCommand ToCommand() => new(Name, Scopes, ExpiresInDays);
}
