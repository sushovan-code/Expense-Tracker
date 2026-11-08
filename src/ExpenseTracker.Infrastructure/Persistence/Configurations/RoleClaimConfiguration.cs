using ExpenseTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.ToTable("RoleClaims", AuthSchema.Name);
        builder.Property(c => c.ClaimType).HasMaxLength(256);
        builder.Property(c => c.ClaimValue).HasMaxLength(1024);
    }
}
