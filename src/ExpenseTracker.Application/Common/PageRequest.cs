using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Application.Common;

/// <summary>Requested page; normalised by <see cref="Paging.Resolve"/>.</summary>
public readonly record struct PageRequest(int? Page, int? PageSize);
