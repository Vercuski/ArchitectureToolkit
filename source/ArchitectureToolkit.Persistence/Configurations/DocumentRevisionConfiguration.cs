using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Persistence.Configurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchitectureToolkit.Persistence.Configurations;

public sealed class DocumentRevisionConfiguration : IEntityTypeConfiguration<DocumentRevision>
{
    public void Configure(EntityTypeBuilder<DocumentRevision> builder)
    {
        builder.ToTable("DOCUMENT_REVISION");
        builder.HasKey(dr => dr.Id);

        // DocumentId's FK and cascade-delete behavior are configured from
        // the ProjectDocument side (ProjectDocumentConfiguration.Revisions)
        // — not repeated here.

        builder.Property(dr => dr.Version)
            .HasConversion<VersionNumberConverter>()
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(dr => dr.BumpType)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(dr => dr.Content)
            .IsRequired();

        builder.Property(dr => dr.AuthorId)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(dr => dr.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(dr => dr.CreatedAt)
            .IsRequired();
    }
}
