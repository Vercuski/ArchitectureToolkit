using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

/// <summary>
/// Changes an existing member's role. Authorized to existing Owners only.
/// See UpdateProjectMemberRoleCommandHandler for the guard against demoting
/// the last remaining Owner — the ProjectRole equivalent of
/// PromoteUserCommandHandler's last-architect guard.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record UpdateProjectMemberRoleCommand(Guid CallerUserId, Guid ProjectId, Guid TargetUserId, ProjectRole NewRole)
    : IMediatRCommandRequest<Result<ProjectMemberDto>>;
