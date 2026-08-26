using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Projects;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record ListProjectMembersQuery(Guid CallerUserId, Guid ProjectId)
    : IMediatRQueryRequest<Result<IReadOnlyCollection<ProjectMemberDto>>>;
