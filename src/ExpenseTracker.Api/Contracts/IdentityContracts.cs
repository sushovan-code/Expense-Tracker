using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

// ---- Authentication ----------------------------------------------------------------------------

public sealed record LoginRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength)] string Password)
{
    public LoginCommand ToCommand() => new(Email, Password);
}

/// <param name="AccessToken">Send as <c>Authorization: Bearer &lt;token&gt;</c>. The refresh token is set as an HttpOnly cookie.</param>
/// <param name="ExpiresIn">Access token lifetime in seconds.</param>
public sealed record AuthResponse(string AccessToken, string TokenType, int ExpiresIn, DateTimeOffset ExpiresAt, CurrentUserDto User)
{
    public static AuthResponse From(AuthSession session, DateTimeOffset now) => new(
        session.AccessToken, "Bearer", (int)Math.Max(0, (session.AccessTokenExpiresAt - now).TotalSeconds),
        session.AccessTokenExpiresAt, session.User);
}

public sealed record ChangePasswordRequest(
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength)] string CurrentPassword,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string NewPassword)
{
    public ChangePasswordCommand ToCommand() => new(CurrentPassword, NewPassword);
}

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

// ---- User administration -----------------------------------------------------------------------

public sealed record UserListQuery(
    [property: Description("Matches email or display name.")] string? Search,
    [property: Description("Only users with this role.")] string? Role,
    [property: Range(1, int.MaxValue), Description("1-based page number (default 1).")] int? Page,
    [property: Range(1, Paging.MaxPageSize), Description("Items per page, 1–100 (default 20).")] int? PageSize,
    [property: Description("name (default), email, created or lastlogin.")] string? SortBy,
    [property: Description("asc (default) or desc.")] string? SortDir)
{
    public UserListRequest ToRequest() => new(Search, Role, new PageRequest(Page, PageSize), SortBy, SortDir);
}

public sealed record CreateUserRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(ContractLimits.DisplayNameMaxLength, MinimumLength = 1)] string DisplayName,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string Password,
    [property: Required, MinLength(1)] string[] Roles)
{
    public CreateUserCommand ToCommand() => new(Email, DisplayName, Password, Roles);
}

public sealed record UpdateUserRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(ContractLimits.DisplayNameMaxLength, MinimumLength = 1)] string DisplayName,
    [property: Description("False disables sign-in and ends the user's sessions and API keys.")] bool IsActive)
{
    public UpdateUserCommand ToCommand() => new(Email, DisplayName, IsActive);
}

public sealed record SetUserRolesRequest([property: Required, MinLength(1)] string[] Roles);

public sealed record UserClaimRequest(
    [property: Required, StringLength(ContractLimits.ClaimTypeMaxLength, MinimumLength = 1), Description("e.g. permission, department.")] string Type,
    [property: Required, StringLength(ContractLimits.ClaimValueMaxLength, MinimumLength = 1)] string Value)
{
    public ClaimDto ToClaim() => new(Type, Value);
}

public sealed record ResetPasswordRequest([property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string NewPassword);

// ---- Role administration -----------------------------------------------------------------------

public sealed record SetRolePermissionsRequest([property: Required] string[] Permissions);
