using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.ArchitectureTests;

/// <summary>Guards the family isolation layer so a new entity or query can't quietly bypass it.</summary>
public class FamilyIsolationTests
{
    private const string FamiliesNamespace = "ExpenseTracker.Domain.Families";

    // The only places allowed to skip a query filter, and why.
    private static readonly string[] AllowedIgnoreFilterFiles =
    [
        // Counts every family expense (including private ones) so a category in use can't be deleted.
        "CategoryRepository.cs",
    ];

    [Fact]
    public void Every_family_owned_entity_has_the_Family_query_filter()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new AppDbContext(options, new FamilyContext());

        // Families, members and invites define the boundary itself, so they are looked up by id or token instead.
        var familyOwned = context.Model.GetEntityTypes()
            .Where(t => t.FindProperty("FamilyId") is not null && t.ClrType.Namespace != FamiliesNamespace)
            .ToList();

        familyOwned.ShouldNotBeEmpty();
        foreach (var entity in familyOwned)
        {
            entity.GetDeclaredQueryFilters().Select(f => f.Key).ShouldContain(AppDbContext.FamilyFilter, entity.ClrType.Name);
        }
    }

    [Fact]
    public void Expenses_also_hide_other_peoples_private_rows()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var context = new AppDbContext(options, new FamilyContext());

        var expense = context.Model.GetEntityTypes().Single(t => t.ClrType.Name == "Expense");

        expense.GetDeclaredQueryFilters().Select(f => f.Key).ShouldContain(AppDbContext.PrivateFilter);
    }

    [Fact]
    public void IgnoreQueryFilters_is_only_used_in_the_named_places_and_never_drops_the_Family_filter()
    {
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                              && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            foreach (var line in File.ReadLines(file).Where(l => l.Contains("IgnoreQueryFilters(", StringComparison.Ordinal)))
            {
                var allowed = AllowedIgnoreFilterFiles.Contains(Path.GetFileName(file))
                    && line.Contains("PrivateFilter", StringComparison.Ordinal);
                if (!allowed) violations.Add($"{Path.GetFileName(file)}: {line.Trim()}");
            }
        }

        violations.ShouldBeEmpty();
    }

    private static string SourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src"))) dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("Could not find the repository's src folder."), "src");
    }
}
