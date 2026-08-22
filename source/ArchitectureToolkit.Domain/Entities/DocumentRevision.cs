using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// One immutable, append-only revision of a ProjectDocument's content. Never
/// updated or deleted after creation (Domain Data Model.md §3). The
/// constructor is internal — only ProjectDocument.CreateRevision may create
/// one, so nothing outside this assembly can bypass RevisionHistory's
/// version/concurrency logic.
/// </summary>
public sealed class DocumentRevision : Entity, IRevision
{
    public Guid DocumentId { get; private set; }
    public VersionNumber Version { get; private set; }

    /// <summary>
    /// Null only for a ProjectDocument's very first revision, which has
    /// nothing to have been "bumped" from — it's always seeded at 1.0.0
    /// (ADR-0013), whether created from scratch or seeded from a
    /// TEMPLATE_REVISION.
    /// </summary>
    public BumpType? BumpType { get; private set; }
    public string Content { get; private set; }
    public Guid AuthorId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal DocumentRevision(Guid documentId, VersionNumber version, BumpType? bumpType, string content, Guid authorId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("DocumentId is required.", nameof(documentId));
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }
        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("AuthorId is required.", nameof(authorId));
        }

        DocumentId = documentId;
        Version = version;
        BumpType = bumpType;
        Content = content;
        AuthorId = authorId;
        CreatedAt = DateTime.UtcNow;
    }
}
