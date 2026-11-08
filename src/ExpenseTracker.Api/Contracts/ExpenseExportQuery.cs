using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Api.Contracts;

public sealed record ExpenseExportQuery(DateOnly? From, DateOnly? To, int? CategoryId, string? Search, string? SortBy, string? SortDir)
{
    public ExpenseFilter ToFilter() => new(From, To, CategoryId, Search);
}
