using ArchitectureToolkit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchitectureToolkit.Persistence.Configurations;

public sealed class UserIdentityConfiguration : IEntityTypeConfiguration<UserIdentity>
{
    public void Configure(EntityTypeBuilder<UserIdentity> builder)
    {
        builder.ToTable("USER_IDENTITY");
        builder.HasKey(ui => ui.Id);

        builder.Property(ui => ui.Issuer)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(ui => ui.ExternalSubjectId)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(ui => ui.ProviderLabel)
            .IsRequired()
            .HasMaxLength(128);

        // Uniqueness enforced here, not just documented in the entity's XML
        // comment — the Domain layer alone can't check uniqueness across rows.
        builder.HasIndex(ui => new { ui.Issuer, ui.ExternalSubjectId })
            .IsUnique();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ui => ui.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(ui => ui.LinkedAt)
            .IsRequired();
    }
}
