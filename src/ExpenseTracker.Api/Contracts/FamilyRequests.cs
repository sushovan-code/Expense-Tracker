using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Features.Families;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Families;

namespace ExpenseTracker.Api.Contracts;

// HTTP request shapes for /families. The password rules match the admin "create user" request.

public sealed record CreateFamilyRequest(
    [property: Required, StringLength(Family.NameMaxLength, MinimumLength = 1)] string Name);

public sealed record AddFamilyMemberRequest(
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(ContractLimits.DisplayNameMaxLength, MinimumLength = 1)] string DisplayName,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string Password,
    [property: Description("Owner, Member or Viewer.")] FamilyRole Role)
{
    public AddMemberCommand ToCommand() => new(Email, DisplayName, Password, Role);
}

public sealed record CreateInviteRequest([property: Description("The role the invited person will get.")] FamilyRole Role);

public sealed record ChangeFamilyRoleRequest(FamilyRole Role);

public sealed record JoinFamilyRequest(
    [property: Required, StringLength(ContractLimits.InviteTokenMaxLength)] string Token);

public sealed record AcceptInviteRequest(
    [property: Required, StringLength(ContractLimits.InviteTokenMaxLength)] string Token,
    [property: Required, EmailAddress, StringLength(ContractLimits.EmailMaxLength)] string Email,
    [property: Required, StringLength(ContractLimits.DisplayNameMaxLength, MinimumLength = 1)] string DisplayName,
    [property: Required, StringLength(CredentialLimits.PasswordMaxLength, MinimumLength = CredentialLimits.PasswordMinLength)] string Password)
{
    public AcceptInviteCommand ToCommand() => new(Token, Email, DisplayName, Password);
}
