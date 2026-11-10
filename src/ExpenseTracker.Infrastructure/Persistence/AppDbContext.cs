using ExpenseTracker.Application.Abstractions.Persistence;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Expenses;
using ExpenseTracker.Domain.Families;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Persistence;

/// <summary>Expense data plus the ASP.NET Core Identity store (users, roles, claims) and session/API key tables.</summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, FamilyContext familyContext)
    : IdentityDbContext<AppUser, AppRole, Guid>(options), IUnitOfWork
{
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<FamilyInvite> FamilyInvites => Set<FamilyInvite>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Identity's model first; the configurations in this assembly then rename and extend it.
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Family isolation (a separate layer from the app roles): every query sees only the caller's family.
        // Nobody without a family matches anything. Named filters so one can be skipped on its own.
        modelBuilder.Entity<Expense>()
            .HasQueryFilter(FamilyFilter, e => e.FamilyId == CurrentFamilyId)
            .HasQueryFilter(PrivateFilter, e => !e.IsPrivate || e.CreatedByUserId == CurrentUserId);
        modelBuilder.Entity<Category>()
            .HasQueryFilter(FamilyFilter, c => c.FamilyId == CurrentFamilyId);
    }

    /// <summary>Name of the filter that limits rows to the caller's family.</summary>
    public const string FamilyFilter = "Family";

    /// <summary>Name of the filter that hides other people's private expenses.</summary>
    public const string PrivateFilter = "Private";

    // Read by the query filters; EF turns them into parameters per query.
    private Guid? CurrentFamilyId => familyContext.FamilyId;
    private Guid CurrentUserId => familyContext.UserId;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // SQLite has no DateTimeOffset type: stored as UTC ticks so comparisons and ordering run in SQL (ADR 0001).
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcTicksConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<UtcTicksConverter>();
    }

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);
}
