using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Persistence.Configurations.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArchitectureToolkit.Persistence.Configurations;

public sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable("TEMPLATE");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(t => t.CurrentVersion)
            .HasConversion<NullableVersionNumberConverter>()
            .HasMaxLength(32);

        // Restrict, not Cascade: the one-to-many Template -> TemplateRevision
        // relationship below already cascades revision deletes via
        // TemplateId. A second cascading path through CurrentRevisionId
        // would create multiple cascade paths to the same TemplateRevision
        // row, which EF Core rejects at model-build time. IsRequired(false)
        // because a brand-new Template has no revisions yet.
        builder.HasOne<TemplateRevision>()
            .WithMany()
            .HasForeignKey(t => t.CurrentRevisionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Optimistic concurrency at the database level, on top of the
        // in-memory check in RevisionHistory<T> — see Domain Data Model.md §3
        // and RevisionConflictException's own doc comment for why both exist.
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

        // Revisions is a read-only collection backed by the private
        // `_revisions` field (Template has no public setter) — target the
        // field directly rather than relying on naming-convention discovery.
        builder.HasMany(t => t.Revisions)
            .WithOne()
            .HasForeignKey(tr => tr.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Revisions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
