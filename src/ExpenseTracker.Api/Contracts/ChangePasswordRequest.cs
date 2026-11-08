using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

public sealed record ChangePasswordRequest(
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength)] string CurrentPassword,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string NewPassword)
{
    public ChangePasswordCommand ToCommand() => new(CurrentPassword, NewPassword);
}
