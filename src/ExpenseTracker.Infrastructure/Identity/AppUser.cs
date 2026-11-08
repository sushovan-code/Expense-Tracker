using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity;

// Persistence models for the identity store. They are ASP.NET Core Identity types (not Domain
// entities): the domain is expenses and budgets, identity is a supporting concern behind ports.

public sealed class AppUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";

    /// <summary>Disabled users can't sign in, and their existing sessions and API keys stop working.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
}
