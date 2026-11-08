using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Expenses;
using ExpenseTracker.Domain.Expenses;

namespace ExpenseTracker.Api.Contracts;

public sealed record SummaryQuery(DateOnly? From, DateOnly? To);
