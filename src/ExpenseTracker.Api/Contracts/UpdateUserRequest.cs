using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Features.Identity;

namespace ExpenseTracker.Api.Contracts;

public sealed record UpdateUserRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(ContractLimits.DisplayNameMaxLength, MinimumLength = 1)] string DisplayName,
    [property: Description("False disables sign-in and ends the user's sessions and API keys.")] bool IsActive)
{
    public UpdateUserCommand ToCommand() => new(Email, DisplayName, IsActive);
}
