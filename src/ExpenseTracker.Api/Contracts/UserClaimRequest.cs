using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Application.Security;

namespace ExpenseTracker.Api.Contracts;

public sealed record UserClaimRequest(
    [property: Required, StringLength(ContractLimits.ClaimTypeMaxLength, MinimumLength = 1), Description("e.g. permission, department.")] string Type,
    [property: Required, StringLength(ContractLimits.ClaimValueMaxLength, MinimumLength = 1)] string Value)
{
    public ClaimDto ToClaim() => new(Type, Value);
}
