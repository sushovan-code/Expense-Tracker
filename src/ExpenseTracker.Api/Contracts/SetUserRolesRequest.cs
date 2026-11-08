using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Contracts;

public sealed record SetUserRolesRequest([property: Required, MinLength(1)] string[] Roles);
