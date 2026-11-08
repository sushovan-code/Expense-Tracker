using ExpenseTracker.Application.Abstractions.Identity;
using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Identity;
using ExpenseTracker.Domain.Common;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>Bulk revocation of refresh-token chains.</summary>
internal static class RefreshTokenRevocation
{
    public static Task<int> RevokeSessionAsync(this DbSet<RefreshToken> tokens, Guid sessionId, DateTimeOffset now, CancellationToken ct) =>
        tokens.Where(t => t.SessionId == sessionId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);

    public static Task<int> RevokeUserSessionsAsync(
        this DbSet<RefreshToken> tokens, Guid userId, DateTimeOffset now, CancellationToken ct, Guid? exceptSessionId = null) =>
        tokens.Where(t => t.UserId == userId && t.RevokedAt == null && t.SessionId != exceptSessionId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
}
