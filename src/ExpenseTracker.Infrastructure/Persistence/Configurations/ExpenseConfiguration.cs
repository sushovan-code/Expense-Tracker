using ExpenseTracker.Domain.Expenses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Description).HasMaxLength(Expense.DescriptionMaxLength).UseCollation(SqliteCollations.CaseInsensitive);
        builder.Property(e => e.Notes).HasMaxLength(Expense.NotesMaxLength).UseCollation(SqliteCollations.CaseInsensitive);
        builder.Property(e => e.Amount).HasPrecision(18, 2);
        builder.HasIndex(e => e.Date);

        builder.HasOne(e => e.Category)
            .WithMany(c => c.Expenses)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
