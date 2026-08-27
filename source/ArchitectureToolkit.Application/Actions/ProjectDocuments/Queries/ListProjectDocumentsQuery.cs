using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record ListProjectDocumentsQuery(Guid CallerUserId, Guid ProjectId)
    : IMediatRQueryRequest<Result<IReadOnlyCollection<ProjectDocumentSummaryDto>>>;
