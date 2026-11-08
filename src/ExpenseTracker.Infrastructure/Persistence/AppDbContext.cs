using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Expenses;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>Expense data plus the ASP.NET Core Identity store (users, roles, claims) and session/API key tables.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IUnitOfWork
{
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's model first; the configurations in this assembly then rename and extend it.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no DateTimeOffset type: stored as UTC ticks so comparisons and ordering run in SQL (ADR 0001).
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcTicksConverter>();
    }

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
