using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Features.Families;
using ExpenseTracker.Application.Security;
using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Expenses;
using ExpenseTracker.Domain.Families;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseTracker.Infrastructure.Identity;

/// <summary>
/// Development only: two demo families, so the isolation between families is visible. Needs the demo users.
/// The Sharma family is user@ (Owner), admin@ (Member) and reader@ (Viewer); the Verma family has its own two
/// users. Idempotent: a family that already exists is left alone.
/// </summary>
public sealed partial class DemoFamilySeeder(
    UserManager<AppUser> users,
    AppDbContext dbContext,
    IOptions<IdentityStoreOptions> settings,
    TimeProvider clock,
    ILogger<DemoFamilySeeder> logger)
{
    private const string SharmaName = "Sharma family";
    private const string VermaName = "Verma family";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var seed = settings.Value.Seed;
        if (string.IsNullOrWhiteSpace(seed.AdminEmail) || string.IsNullOrWhiteSpace(seed.DemoPassword)) return;
        var domain = seed.AdminEmail[(seed.AdminEmail.IndexOf('@') + 1)..];

        var created = 0;
        created += await SeedFamilyAsync(SharmaName,
            [($"user@{domain}", FamilyRole.Owner), ($"admin@{domain}", FamilyRole.Member), ($"reader@{domain}", FamilyRole.Viewer)],
            [("Groceries", 2450m, "Food"), ("Metro card", 500m, "Transport")], seed.DemoPassword, cancellationToken);

        var verma = new[] { ($"verma.owner@{domain}", FamilyRole.Owner), ($"verma.member@{domain}", FamilyRole.Member) };
        created += await SeedFamilyAsync(VermaName, verma,
            [("Electricity bill", 1800m, "Utilities"), ("Movie night", 900m, "Entertainment")], seed.DemoPassword, cancellationToken);

        // One person can own several families: user@ also owns a second one (for the parents).
        created += await SeedFamilyAsync("Sharma parents", [($"user@{domain}", FamilyRole.Owner)],
            [("Medicines", 650m, "Health")], seed.DemoPassword, cancellationToken);

        LogSeeded(logger, created);
    }

    private async Task<int> SeedFamilyAsync(
        string name,
        (string Email, FamilyRole Role)[] people,
        (string Description, decimal Amount, string Category)[] expenses,
        string password,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Families.AnyAsync(f => f.Name == name, cancellationToken)) return 0;

        var now = clock.GetUtcNow();
        var family = Family.Create(name, now).Value;
        dbContext.Families.Add(family);

        var owner = Guid.Empty;
        foreach (var (email, role) in people)
        {
            var userId = await FindOrCreateUserAsync(email, password);
            dbContext.FamilyMembers.Add(FamilyMember.Create(family.Id, userId, role, now));
            if (role == FamilyRole.Owner) owner = userId;
        }

        var categories = DefaultCategories.All
            .Select(c => Category.Create(c.Name, c.Color, null, family.Id).Value)
            .ToDictionary(c => c.Name);
        dbContext.Categories.AddRange(categories.Values);

        var today = DateOnly.FromDateTime(now.UtcDateTime);
        foreach (var (description, amount, category) in expenses)
        {
            var details = new ExpenseDetails(description, amount, today, categories[category].Id, null);
            dbContext.Expenses.Add(Expense.Create(details, now, family.Id, owner).Value);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private async Task<Guid> FindOrCreateUserAsync(string email, string password)
    {
        if (await users.FindByEmailAsync(email) is { } existing) return existing.Id;

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = DisplayNameOf(email),
            IsActive = true,
            CreatedAt = clock.GetUtcNow(),
        };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Could not seed user {email}: {string.Join(" ", result.Errors.Select(e => e.Description))}");

        await users.AddToRoleAsync(user, AppRoles.User);
        return user.Id;
    }

    private static string DisplayNameOf(string email) => $"Demo {email[..email.IndexOf('@')].Replace('.', ' ')}";

    [LoggerMessage(EventId = LogEvents.IdentitySeeded, Level = LogLevel.Information, Message = "Demo families seeded: {FamiliesCreated} created")]
    private static partial void LogSeeded(ILogger logger, int familiesCreated);
}
