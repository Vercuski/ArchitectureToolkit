using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

/// <summary>
/// Lists a document's revision history — lightweight summaries (no
/// content); use GetDocumentRevisionQuery for a specific historical
/// revision's full content. Authorized to any member of the document's
/// project (Viewer+), same as GetProjectDocumentQuery.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record ListDocumentRevisionsQuery(Guid CallerUserId, Guid DocumentId)
    : IMediatRQueryRequest<Result<IReadOnlyCollection<DocumentRevisionDto>>>;
