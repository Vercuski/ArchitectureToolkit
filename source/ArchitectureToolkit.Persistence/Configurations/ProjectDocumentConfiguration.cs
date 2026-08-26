using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Persistence.Configurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchitectureToolkit.Persistence.Configurations;

public sealed class ProjectDocumentConfiguration : IEntityTypeConfiguration<ProjectDocument>
{
    public void Configure(EntityTypeBuilder<ProjectDocument> builder)
    {
        builder.ToTable("PROJECT_DOCUMENT");
        builder.HasKey(pd => pd.Id);

        builder.Property(pd => pd.Title)
            .IsRequired()
            .HasMaxLength(256);

        // Cascade here, unlike Template's Restrict on CATEGORY: a
        // PROJECT_DOCUMENT has no existence independent of its PROJECT,
        // whereas TEMPLATE is shared library content owned by no one project.
        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(pd => pd.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(pd => pd.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional — null for a document created from scratch rather than
        // seeded from a template. Restrict rather than Cascade/SetNull:
        // TEMPLATE_REVISION rows are never deleted (append-only), so this
        // is mostly a formality, but Restrict is still the safer default
        // for a lineage-tracking FK.
        builder.HasOne<TemplateRevision>()
            .WithMany()
            .HasForeignKey(pd => pd.SourceTemplateRevisionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.Property(pd => pd.CurrentVersion)
            .HasConversion<NullableVersionNumberConverter>()
            .HasMaxLength(32);

        // Deliberately NOT a database-enforced foreign key — same
        // reasoning as TemplateConfiguration's CurrentRevisionId: this is
        // an application-maintained pointer whose validity RevisionHistory<T>
        // already guarantees by construction, not a relationship needing
        // DB-level integrity, and enforcing it as a real FK creates an
        // unresolvable circular relationship with the necessary
        // DocumentRevision.DocumentId FK. Confirmed live: Template's
        // identical configuration produced a real PostgreSQL 23503
        // violation on its first revision, not just a client-side EF Core
        // ordering quirk — the same fix applies here before this entity's
        // first revision is ever created.
        builder.Property(pd => pd.CurrentRevisionId);

        // UseXminAsConcurrencyToken() was removed in Npgsql.EntityFrameworkCore.PostgreSQL 7.0.
        // This is the confirmed replacement -- the deprecated method's own internal
        // source did exactly this. IMPORTANT: the first migration generated against this
        // will almost certainly include a redundant migrationBuilder.AddColumn(name: "xmin", ...)
        // statement (and a matching DropColumn in Down()) -- xmin already exists implicitly
        // on every PostgreSQL row as a system column, so applying that statement as written
        // fails with "42701: column name \"xmin\" conflicts with a system column name". Delete
        // both the AddColumn and DropColumn calls from the generated migration by hand; the
        // shadow-property mapping below still works correctly at runtime regardless.
        builder.Property<uint>("xmin")
            .HasColumnType("xid")
            .IsRowVersion();

        builder.HasMany(pd => pd.Revisions)
            .WithOne()
            .HasForeignKey(dr => dr.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(pd => pd.Revisions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
