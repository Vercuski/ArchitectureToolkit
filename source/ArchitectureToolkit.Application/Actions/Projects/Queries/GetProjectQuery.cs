using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Projects;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
/// <param name="ProjectId">The project to fetch.</param>
public sealed record GetProjectQuery(Guid CallerUserId, Guid ProjectId)
    : IMediatRQueryRequest<Result<ProjectDto>>;
