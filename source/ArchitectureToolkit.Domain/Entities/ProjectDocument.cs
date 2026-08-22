using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// One project's instance of a document — e.g. a specific project's copy of
/// the Architecture Vision, optionally seeded from a TEMPLATE_REVISION but
/// versioned entirely independently after that (Domain Data Model.md §2).
/// Revisioning is delegated to a transient RevisionHistory built from this
/// document's own persisted CurrentRevisionId/CurrentVersion state (ADR-0007)
/// — composition, not a shared base class with Template.
///
/// Unlike Template (gated by USER.system_role, ADR-0006), access here is
/// governed entirely by PROJECT_MEMBER.role — any project member with edit
/// rights may call CreateRevision, regardless of system_role. Enforcing that
/// is an Application-layer authorization concern, not this class's job.
/// </summary>
public sealed class ProjectDocument : Entity
{
    public Guid ProjectId { get; private set; }
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// The TEMPLATE_REVISION this document was seeded from, if any. Null for
    /// a document created from scratch. Points at a specific revision, not
    /// just the owning Template, so lineage survives the source template
    /// being revised later (Domain Data Model.md §3).
    /// </summary>
    public Guid? SourceTemplateRevisionId { get; private set; }

    public string Title { get; private set; }
    public Guid? CurrentRevisionId { get; private set; }
    public VersionNumber? CurrentVersion { get; private set; }

    private readonly List<DocumentRevision> _revisions = [];
    public IReadOnlyCollection<DocumentRevision> Revisions => _revisions.AsReadOnly();

    public ProjectDocument(Guid projectId, Guid categoryId, string title, Guid? sourceTemplateRevisionId = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("ProjectId is required.", nameof(projectId));
        }
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        ProjectId = projectId;
        CategoryId = categoryId;
        Title = title;
        SourceTemplateRevisionId = sourceTemplateRevisionId;
    }

    /// <summary>
    /// Creates and records a new revision. Throws RevisionConflictException if
    /// expectedCurrentRevisionId doesn't match CurrentRevisionId — i.e.
    /// someone else already saved a newer revision since the caller last read
    /// this document. bumpType is ignored for the very first revision, which
    /// is always seeded at 1.0.0 (ADR-0013) — see RevisionHistory{TRevision}.
    /// </summary>
    public DocumentRevision CreateRevision(Guid? expectedCurrentRevisionId, BumpType? bumpType, string content, Guid authorId)
    {
        var revisionHistory = new RevisionHistory<DocumentRevision>(CurrentRevisionId, CurrentVersion);

        var revision = revisionHistory.AppendRevision(
            expectedCurrentRevisionId,
            bumpType,
            content,
            authorId,
            (version, revisionContent, revisionAuthorId) =>
                new DocumentRevision(Id, version, bumpType, revisionContent, revisionAuthorId));

        CurrentRevisionId = revisionHistory.CurrentRevisionId;
        CurrentVersion = revisionHistory.CurrentVersion;
        _revisions.Add(revision);

        return revision;
    }
}
