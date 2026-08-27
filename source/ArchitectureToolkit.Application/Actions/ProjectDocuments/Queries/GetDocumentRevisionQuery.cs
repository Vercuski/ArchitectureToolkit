using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
/// <param name="DocumentId">
/// Included for URL-nesting/scoping consistency and validated against the
/// revision — a revisionId that exists but belongs to a different document
/// returns NotFound rather than leaking cross-document content.
/// </param>
public sealed record GetDocumentRevisionQuery(Guid CallerUserId, Guid DocumentId, Guid RevisionId)
    : IMediatRQueryRequest<Result<DocumentRevisionDetailDto>>;
