using ExpenseTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private const int HexColorLength = 7; // #rrggbb

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength).UseCollation(SqliteCollations.CaseInsensitive);
        builder.Property(c => c.Color).HasMaxLength(HexColorLength);
        builder.Property(c => c.MonthlyBudget).HasPrecision(18, 2);
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Navigation(c => c.Expenses).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Default categories (anonymous objects: the entity has no public constructor). Fixed IDs keep the
        // seed stable across migrations.
        builder.HasData(
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000001"), Name = "Food", Color = "#f97316" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000002"), Name = "Transport", Color = "#3b82f6" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000003"), Name = "Housing", Color = "#8b5cf6" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000004"), Name = "Utilities", Color = "#06b6d4" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000005"), Name = "Entertainment", Color = "#ec4899" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000006"), Name = "Health", Color = "#22c55e" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000007"), Name = "Shopping", Color = "#eab308" },
            new { Id = Guid.Parse("0192f0a0-0000-7000-8000-000000000008"), Name = "Other", Color = "#64748b" });
    }
}
