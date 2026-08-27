using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
/// <param name="DocumentId">The document to fetch.</param>
public sealed record GetProjectDocumentQuery(Guid CallerUserId, Guid DocumentId)
    : IMediatRQueryRequest<Result<ProjectDocumentDetailDto>>;
