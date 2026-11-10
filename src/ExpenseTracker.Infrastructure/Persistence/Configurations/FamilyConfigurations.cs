using ExpenseTracker.Domain.Families;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations;

internal sealed class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.Property(f => f.Id).ValueGeneratedNever();
        builder.Property(f => f.Name).HasMaxLength(Family.NameMaxLength);
    }
}

internal sealed class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> builder)
    {
        builder.Property(m => m.Id).ValueGeneratedNever();
        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        // A person is in a family once. They may own several families; being a Member or Viewer in more than
        // one is refused by FamilyPolicy, not by the schema.
        builder.HasIndex(m => new { m.FamilyId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);

        builder.HasOne<Family>().WithMany().HasForeignKey(m => m.FamilyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class FamilyInviteConfiguration : IEntityTypeConfiguration<FamilyInvite>
{
    private const int TokenHashLength = 64; // SHA-256 as hex

    public void Configure(EntityTypeBuilder<FamilyInvite> builder)
    {
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Role).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.TokenHash).HasMaxLength(TokenHashLength);

        builder.HasIndex(i => i.TokenHash).IsUnique();
        builder.HasIndex(i => i.FamilyId);

        builder.HasOne<Family>().WithMany().HasForeignKey(i => i.FamilyId).OnDelete(DeleteBehavior.Cascade);
    }
}
