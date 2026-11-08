using ExpenseTracker.Domain.Categories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).HasMaxLength(Category.NameMaxLength);
        builder.Property(c => c.Color).HasMaxLength(7);
        builder.Property(c => c.MonthlyBudget).HasPrecision(18, 2);
        builder.HasIndex(c => c.Name).IsUnique();

        builder.Navigation(c => c.Expenses).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Default categories (anonymous objects: the entity has no public constructor).
        builder.HasData(
            new { Id = 1, Name = "Food", Color = "#f97316" },
            new { Id = 2, Name = "Transport", Color = "#3b82f6" },
            new { Id = 3, Name = "Housing", Color = "#8b5cf6" },
            new { Id = 4, Name = "Utilities", Color = "#06b6d4" },
            new { Id = 5, Name = "Entertainment", Color = "#ec4899" },
            new { Id = 6, Name = "Health", Color = "#22c55e" },
            new { Id = 7, Name = "Shopping", Color = "#eab308" },
            new { Id = 8, Name = "Other", Color = "#64748b" });
    }
}
