using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Contracts;

public sealed record SetRolePermissionsRequest([property: Required] string[] Permissions);
