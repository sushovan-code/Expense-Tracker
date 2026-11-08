using ExpenseTracker.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

// Identity lives in its own "auth" schema with plain table names (instead of dbo.AspNet*),
// keeping it visibly separate from the expense data.

internal static class AuthSchema
{
    public const string Name = "auth";
}

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users", AuthSchema.Name);
        builder.Property(u => u.DisplayName).HasMaxLength(100);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
    }
}

internal sealed class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("Roles", AuthSchema.Name);
        builder.Property(r => r.Description).HasMaxLength(300);
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTable("UserRoles", AuthSchema.Name);
}

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder)
    {
        builder.ToTable("UserClaims", AuthSchema.Name);
        builder.Property(c => c.ClaimType).HasMaxLength(256);
        builder.Property(c => c.ClaimValue).HasMaxLength(1024);
    }
}

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder)
    {
        builder.ToTable("RoleClaims", AuthSchema.Name);
        builder.Property(c => c.ClaimType).HasMaxLength(256);
        builder.Property(c => c.ClaimValue).HasMaxLength(1024);
    }
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("UserLogins", AuthSchema.Name);
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("UserTokens", AuthSchema.Name);
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens", AuthSchema.Name);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash).HasMaxLength(64);
        builder.Property(t => t.CreatedByIp).HasMaxLength(45);
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.SessionId);
        builder.HasIndex(t => new { t.UserId, t.ExpiresAt });
        builder.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

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
