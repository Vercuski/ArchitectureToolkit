using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

/// <summary>Adds a user to a project. Authorized to existing Owners only.</summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
/// <param name="ProjectId">The project to add the member to.</param>
/// <param name="TargetUserId">The user being added.</param>
/// <param name="Role">The role to grant the new member.</param>
public sealed record AddProjectMemberCommand(Guid CallerUserId, Guid ProjectId, Guid TargetUserId, ProjectRole Role)
    : IMediatRCommandRequest<Result<ProjectMemberDto>>;
