using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Api.Contracts;

public sealed record BulkDeleteRequest(
    [property: Required, MinLength(1), MaxLength(500), Description("Ids of the expenses to delete (max 500).")]
    int[] Ids);
