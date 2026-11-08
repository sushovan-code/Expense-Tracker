using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Contracts;

public sealed record BudgetQuery(
    [property: Range(ContractLimits.MinBudgetYear, ContractLimits.MaxBudgetYear), Description("Year (defaults to the current year).")] int? Year,
    [property: Range(ContractLimits.FirstMonth, ContractLimits.LastMonth), Description("Month 1–12 (defaults to the current month).")] int? Month);
