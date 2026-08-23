using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// A reusable document definition in the template library. Revisioning is
/// delegated to a transient RevisionHistory built from this Template's own
/// persisted CurrentRevisionId/CurrentVersion state (ADR-0007) — composition,
/// not a shared base class with ProjectDocument.
///
/// Only an Architect-role User may call CreateRevision (ADR-0006) — enforcing
/// that is an Application-layer authorization concern, not this class's job.
/// </summary>
public sealed class Template : Entity
{
    public Guid CategoryId { get; private set; }
    public string Name { get; private set; }
    public Guid? CurrentRevisionId { get; private set; }
    public VersionNumber? CurrentVersion { get; private set; }

    private readonly List<TemplateRevision> _revisions = new();
    public IReadOnlyCollection<TemplateRevision> Revisions => _revisions.AsReadOnly();

    public Template(Guid categoryId, string name)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("CategoryId is required.", nameof(categoryId));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        CategoryId = categoryId;
        Name = name;
    }

    /// <summary>
    /// Creates and records a new revision. Throws RevisionConflictException if
    /// expectedCurrentRevisionId doesn't match CurrentRevisionId — i.e. someone
    /// else already saved a newer revision since the caller last read this
    /// Template. bumpType is ignored for the very first revision, which is
    /// always seeded at 1.0.0 (ADR-0013) — see RevisionHistory{TRevision}.
    /// </summary>
    public TemplateRevision CreateRevision(Guid? expectedCurrentRevisionId, BumpType? bumpType, string content, Guid authorId)
    {
        var revisionHistory = new RevisionHistory<TemplateRevision>(CurrentRevisionId, CurrentVersion);

        var revision = revisionHistory.AppendRevision(
            expectedCurrentRevisionId,
            bumpType,
            content,
            authorId,
            (version, resolvedBumpType, revisionContent, revisionAuthorId) =>
                new TemplateRevision(Id, version, resolvedBumpType, revisionContent, revisionAuthorId));

        CurrentRevisionId = revisionHistory.CurrentRevisionId;
        CurrentVersion = revisionHistory.CurrentVersion;
        _revisions.Add(revision);

        return revision;
    }
}
