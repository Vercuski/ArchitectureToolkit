using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Projects;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record ListProjectsQuery(Guid CallerUserId)
    : IMediatRQueryRequest<Result<IReadOnlyCollection<ProjectDto>>>;
