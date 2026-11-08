using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    private const int DisplayNameMaxLength = 100;

    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.Property(u => u.DisplayName).HasMaxLength(DisplayNameMaxLength).UseCollation(SqliteCollations.CaseInsensitive);
        builder.Property(u => u.Email).UseCollation(SqliteCollations.CaseInsensitive);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
    }
}
