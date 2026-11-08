using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Api.Contracts;

// HTTP request shapes. Attributes give fast, framework-level 400s and document the API;
// the domain enforces the same rules again, so other entry points stay safe too.

public sealed record ExpenseRequest(
    [property: Required, StringLength(Expense.DescriptionMaxLength, MinimumLength = 1), Description("What the money was spent on.")]
    string Description,
    [property: Range(ContractLimits.SmallestAmount, Expense.MaxWholeAmount), Description("Amount spent, up to two decimal places.")]
    decimal Amount,
    [property: Description("Date of the expense (YYYY-MM-DD).")]
    DateOnly Date,
    [property: Range(1, int.MaxValue), Description("Id of an existing category.")]
    int CategoryId,
    [property: StringLength(Expense.NotesMaxLength)]
    string? Notes)
{
    public ExpenseCommand ToCommand() => new(Description, Amount, Date, CategoryId, Notes);
}
