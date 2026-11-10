using ExpenseTracker.Domain.Categories;
using ExpenseTracker.Domain.Families;
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
        // Names are unique within a family, not across the app.
        builder.HasIndex(c => new { c.FamilyId, c.Name }).IsUnique();

        builder.HasOne<Family>().WithMany().HasForeignKey(c => c.FamilyId).OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(c => c.Expenses).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
