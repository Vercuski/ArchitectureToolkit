using ArchitectureToolkit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchitectureToolkit.Persistence.Configurations;

public sealed class DocumentAttachmentConfiguration : IEntityTypeConfiguration<DocumentAttachment>
{
    public void Configure(EntityTypeBuilder<DocumentAttachment> builder)
    {
        builder.ToTable("DOCUMENT_ATTACHMENT");
        builder.HasKey(a => a.Id);

        // Cascade, same reasoning as ProjectDocumentConfiguration's own
        // ProjectId FK: an attachment has no existence independent of its
        // PROJECT. Deleting a project should reclaim its uploaded files'
        // metadata along with everything else scoped to it — the on-disk
        // bytes themselves aren't reclaimed automatically by this (EF
        // Core cascades only affect the database), which is a known,
        // accepted gap rather than an oversight; see IAttachmentStorage's
        // doc comment.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(a => a.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(260); // matches Windows MAX_PATH's filename component; generous for any real upload

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.StorageKey)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(a => a.SizeBytes)
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(a => a.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.UploadedAt)
            .IsRequired();
    }
}
