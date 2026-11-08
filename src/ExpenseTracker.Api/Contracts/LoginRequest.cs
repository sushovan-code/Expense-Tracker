using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

public sealed record LoginRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength)] string Password)
{
    public LoginCommand ToCommand() => new(Email, Password);
}
