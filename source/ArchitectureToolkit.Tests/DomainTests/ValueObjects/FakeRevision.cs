using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.ValueObjects;

/// <summary>
/// Minimal IRevision implementation used only to exercise RevisionHistory{TRevision}
/// in isolation, independent of the real TemplateRevision/DocumentRevision types —
/// whose constructors are internal to the Domain assembly by design (see their own
/// doc comments: "so nothing outside this assembly can bypass RevisionHistory's
/// version/concurrency logic"), and therefore cannot be constructed directly from
/// this project. This mirrors ADR-0007's own stated intent that RevisionHistory{T}
/// be testable without depending on either real aggregate.
/// </summary>
internal sealed class FakeRevision : IRevision
{
    public Guid Id { get; }
    public VersionNumber Version { get; }
    public BumpType? BumpType { get; }
    public string Content { get; }
    public Guid AuthorId { get; }

    public FakeRevision(VersionNumber version, BumpType? bumpType, string content, Guid authorId)
    {
        Id = Guid.NewGuid();
        Version = version;
        BumpType = bumpType;
        Content = content;
        AuthorId = authorId;
    }
}
