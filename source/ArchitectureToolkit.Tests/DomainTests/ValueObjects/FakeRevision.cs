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
internal sealed class FakeRevision(VersionNumber version, BumpType? bumpType, string content, Guid authorId) : IRevision
{
    public Guid Id { get; } = Guid.NewGuid();
    public VersionNumber Version { get; } = version;
    public BumpType? BumpType { get; } = bumpType;
    public string Content { get; } = content;
    public Guid AuthorId { get; } = authorId;
}
