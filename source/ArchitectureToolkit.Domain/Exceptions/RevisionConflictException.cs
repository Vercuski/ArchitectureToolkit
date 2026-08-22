namespace ArchitectureToolkit.Domain.Exceptions;

/// <summary>
/// Thrown when a revision is appended against a stale expected current-revision Id —
/// i.e. someone else already saved a newer revision since the caller last read this
/// aggregate's state. This is an in-memory, domain-level check performed inside
/// <see cref="ArchitectureToolkit.Domain.ValueObjects.RevisionHistory{TRevision}"/>.
///
/// It is deliberately separate from, not a replacement for, the database-level guard:
/// PostgreSQL's `xmin` column, configured as an EF Core concurrency token (Domain Data
/// Model.md §3), catches races between concurrent transactions that this in-memory
/// check cannot see (e.g. two requests against two different DbContext instances).
/// </summary>
public sealed class RevisionConflictException(Guid? expectedRevisionId, Guid? actualRevisionId) : Exception(
        $"Revision conflict: expected current revision '{expectedRevisionId?.ToString() ?? "(none)"}' " +
            $"but the current revision is '{actualRevisionId?.ToString() ?? "(none)"}'. " +
            "Someone else has already saved a newer revision.")
{
    public Guid? ExpectedRevisionId { get; } = expectedRevisionId;
    public Guid? ActualRevisionId { get; } = actualRevisionId;
}
