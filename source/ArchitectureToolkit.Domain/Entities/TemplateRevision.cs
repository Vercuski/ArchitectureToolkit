using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// One immutable, append-only revision of a Template's content. Never updated
/// or deleted after creation (Domain Data Model.md §3). The constructor is
/// internal — only Template.CreateRevision may create one, so nothing outside
/// this assembly can bypass RevisionHistory's version/concurrency logic.
/// </summary>
public sealed class TemplateRevision : Entity, IRevision
{
    public Guid TemplateId { get; private set; }
    public VersionNumber Version { get; private set; }

    /// <summary>
    /// Null only for a Template's very first revision, which has nothing to
    /// have been "bumped" from — it's always seeded at 1.0.0 (ADR-0013).
    /// </summary>
    public BumpType? BumpType { get; private set; }
    public string Content { get; private set; }
    public Guid AuthorId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    internal TemplateRevision(Guid templateId, VersionNumber version, BumpType? bumpType, string content, Guid authorId)
    {
        if (templateId == Guid.Empty)
        {
            throw new ArgumentException("TemplateId is required.", nameof(templateId));
        }
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }
        if (authorId == Guid.Empty)
        {
            throw new ArgumentException("AuthorId is required.", nameof(authorId));
        }

        TemplateId = templateId;
        Version = version;
        BumpType = bumpType;
        Content = content;
        AuthorId = authorId;
        CreatedAt = DateTime.UtcNow;
    }
}
