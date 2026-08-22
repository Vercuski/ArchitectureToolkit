using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.Exceptions;

namespace ArchitectureToolkit.Domain.ValueObjects;

/// <summary>
/// Encapsulates the append-only, SemVer-versioned, optimistic-concurrency-checked
/// revisioning behavior shared by Template/TemplateRevision and ProjectDocument/
/// DocumentRevision (ADR-0007). Composed into each aggregate root rather than shared
/// via a base class — Template and ProjectDocument remain fully independent aggregates
/// beyond holding an instance of this.
/// </summary>
/// <typeparam name="TRevision">
/// The concrete revision type (e.g. TemplateRevision, DocumentRevision).
/// </typeparam>
public sealed class RevisionHistory<TRevision>
    where TRevision : IRevision
{
    public Guid? CurrentRevisionId { get; private set; }
    public VersionNumber? CurrentVersion { get; private set; }

    /// <param name="currentRevisionId">
    /// Null for a brand-new aggregate with no revisions yet; otherwise the Id of the
    /// most recent revision, as reconstructed from persisted state.
    /// </param>
    /// <param name="currentVersion">Must be set if, and only if, <paramref name="currentRevisionId"/> is.</param>
    public RevisionHistory(Guid? currentRevisionId = null, VersionNumber? currentVersion = null)
    {
        if (currentRevisionId is null != currentVersion is null)
        {
            throw new ArgumentException(
                "CurrentRevisionId and CurrentVersion must both be set, or both be null.");
        }

        CurrentRevisionId = currentRevisionId;
        CurrentVersion = currentVersion;
    }

    /// <summary>
    /// Creates and records a new revision.
    /// </summary>
    /// <param name="expectedCurrentRevisionId">
    /// The revision Id the caller last read. Must equal <see cref="CurrentRevisionId"/>
    /// — both null, for the very first revision, or both the same value — or this
    /// throws <see cref="RevisionConflictException"/>.
    /// </param>
    /// <param name="bumpType">
    /// The SemVer bump to apply. Ignored for the very first revision (there is no prior
    /// version to bump from) — that revision is always <see cref="VersionNumber.Initial"/>
    /// (ADR-0013). Required for every revision after the first.
    /// </param>
    /// <param name="content">The revision's content.</param>
    /// <param name="authorId">The authoring user's Id.</param>
    /// <param name="factory">
    /// Builds the concrete <typeparamref name="TRevision"/> from the resolved version,
    /// content, and author — the caller supplies any type-specific data (e.g. the owning
    /// Template's Id) via closure.
    /// </param>
    public TRevision AppendRevision(
        Guid? expectedCurrentRevisionId,
        BumpType? bumpType,
        string content,
        Guid authorId,
        Func<VersionNumber, string, Guid, TRevision> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (expectedCurrentRevisionId != CurrentRevisionId)
        {
            throw new RevisionConflictException(expectedCurrentRevisionId, CurrentRevisionId);
        }

        VersionNumber newVersion;
        if (CurrentVersion is null)
        {
            // First revision ever: nothing to bump from. Every template/document is
            // seeded at 1.0.0 regardless of the requested bumpType (ADR-0013).
            newVersion = VersionNumber.Initial;
        }
        else
        {
            if (bumpType is null)
            {
                throw new ArgumentNullException(
                    nameof(bumpType),
                    "A bump type is required for every revision after the first.");
            }
            newVersion = CurrentVersion.Value.Bump(bumpType.Value);
        }

        var revision = factory(newVersion, content, authorId);

        CurrentRevisionId = revision.Id;
        CurrentVersion = revision.Version;

        return revision;
    }
}
