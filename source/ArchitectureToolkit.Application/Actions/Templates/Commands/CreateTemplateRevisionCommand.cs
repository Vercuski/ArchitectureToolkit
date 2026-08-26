using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Templates.Commands;

/// <summary>
/// Adds a new revision to an existing Template. Architect-only (ADR-0006).
/// <paramref name="ExpectedCurrentRevisionId"/> implements optimistic
/// concurrency (Domain Data Model.md §3): pass the revision Id the caller
/// last read the template at. A mismatch — someone else already saved a
/// newer revision — surfaces as a Conflict, whether caught in-memory by
/// RevisionHistory{T} or by the database's own xmin-based race guard (see
/// CommandDbContext.SaveChangesAsync).
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record CreateTemplateRevisionCommand(
    Guid CallerUserId, Guid TemplateId, Guid? ExpectedCurrentRevisionId, BumpType? BumpType, string Content)
    : IMediatRCommandRequest<Result<TemplateRevisionDto>>;
