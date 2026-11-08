using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Contracts;

public sealed record BulkDeleteRequest(
    [property: Required, MinLength(1), MaxLength(500), Description("Ids of the expenses to delete (max 500).")]
    Guid[] Ids);
