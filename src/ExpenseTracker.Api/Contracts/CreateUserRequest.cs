using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

public sealed record CreateUserRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(ContractLimits.DisplayNameMaxLength, MinimumLength = 1)] string DisplayName,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string Password,
    [property: Required, MinLength(1)] string[] Roles)
{
    public CreateUserCommand ToCommand() => new(Email, DisplayName, Password, Roles);
}
