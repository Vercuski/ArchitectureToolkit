using ArchitectureToolkit.Application.Abstractions;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

/// <summary>
/// Removes a member from a project. Authorized to existing Owners only.
/// See RemoveProjectMemberCommandHandler for the guard against removing
/// the last remaining Owner.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record RemoveProjectMemberCommand(Guid CallerUserId, Guid ProjectId, Guid TargetUserId)
    : IMediatRCommandRequest<Result<bool>>;
