using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;

namespace ExpenseTracker.Api.Contracts;

public sealed record UserListQuery(
    [property: Description("Matches email or display name.")] string? Search,
    [property: Description("Only users with this role.")] string? Role,
    [property: Range(1, int.MaxValue), Description("1-based page number (default 1).")] int? Page,
    [property: Range(1, Paging.MaxPageSize), Description("Items per page, 1–100 (default 20).")] int? PageSize,
    [property: Description("name (default), email, created or lastlogin.")] string? SortBy,
    [property: Description("asc (default) or desc.")] string? SortDir)
{
    public UserListRequest ToRequest() => new(Search, Role, new PageRequest(Page, PageSize), SortBy, SortDir);
}
