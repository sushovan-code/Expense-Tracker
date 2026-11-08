using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Common;

/// <summary>One page of results plus what a grid needs to render its pager.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Requested page; normalised by <see cref="Paging.Resolve"/>.</summary>
public readonly record struct PageRequest(int? Page, int? PageSize);

/// <summary>A validated, allow-listed sort.</summary>
public readonly record struct SortSpec(string Field, bool Descending)
{
    public string Direction => Descending ? "desc" : "asc";
}

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Normalises page input and clamps the page to the last one that has data.</summary>
    public static (int Page, int PageSize) Resolve(PageRequest request, int totalCount)
    {
        var size = Math.Clamp(request.PageSize ?? DefaultPageSize, 1, MaxPageSize);
        var lastPage = Math.Max(1, (int)Math.Ceiling(totalCount / (double)size));
        return (Math.Clamp(request.Page ?? 1, 1, lastPage), size);
    }

    /// <summary>Validates a sort request against an allow-list (never sort by arbitrary client input).</summary>
    public static Result<SortSpec> ResolveSort(
        string? sortBy, string? sortDir, string defaultField, bool defaultDescending, IReadOnlyCollection<string> allowed)
    {
        var field = string.IsNullOrWhiteSpace(sortBy) ? defaultField : sortBy.Trim().ToLowerInvariant();
        if (!allowed.Contains(field))
        {
            return Error.Validation("sortBy", $"Sort by one of: {string.Join(", ", allowed)}.");
        }

        var direction = sortDir?.Trim().ToLowerInvariant();
        if (direction is not (null or "" or "asc" or "desc"))
        {
            return Error.Validation("sortDir", "Sort direction must be 'asc' or 'desc'.");
        }

        var descending = direction switch
        {
            "asc" => false,
            "desc" => true,
            _ => field == defaultField && defaultDescending,
        };
        return new SortSpec(field, descending);
    }
}
