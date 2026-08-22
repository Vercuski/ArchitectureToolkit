using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Persistence.Configurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchitectureToolkit.Persistence.Configurations;

public sealed class TemplateRevisionConfiguration : IEntityTypeConfiguration<TemplateRevision>
{
    public void Configure(EntityTypeBuilder<TemplateRevision> builder)
    {
        builder.ToTable("TEMPLATE_REVISION");
        builder.HasKey(tr => tr.Id);

        // TemplateId's FK and cascade-delete behavior are configured from
        // the Template side (TemplateConfiguration.Revisions) — not
        // repeated here, to avoid configuring the same relationship twice.

        builder.Property(tr => tr.Version)
            .HasConversion<VersionNumberConverter>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(tr => tr.BumpType)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(tr => tr.Content)
            .IsRequired();
        // No HasMaxLength: this is markdown template content, mapped to
        // PostgreSQL `text` by Npgsql's default unbounded-string convention.

        builder.Property(tr => tr.AuthorId)
            .IsRequired();

        // Restrict: deleting a USER must not silently delete the append-only
        // audit trail of everything they authored (Domain Data Model.md §3).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tr => tr.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(tr => tr.CreatedAt)
            .IsRequired();
    }
}
