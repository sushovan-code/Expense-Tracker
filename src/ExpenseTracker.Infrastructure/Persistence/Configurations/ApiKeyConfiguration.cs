using ExpenseTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys", AuthSchema.Name);
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Property(k => k.Name).HasMaxLength(100);
        builder.Property(k => k.Prefix).HasMaxLength(16);
        builder.Property(k => k.KeyHash).HasMaxLength(64);
        builder.Property(k => k.Scopes).HasMaxLength(1000);
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => k.UserId);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
