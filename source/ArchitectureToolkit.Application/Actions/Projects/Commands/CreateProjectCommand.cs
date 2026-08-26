using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Projects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

/// <summary>
/// Creates a new Project and grants the caller ProjectRole.Owner on it, in
/// the same SaveChangesAsync. This is the only way a project ever gets its
/// first member: AddProjectMemberCommand requires an existing Owner to
/// call it, so without this, a brand-new project would have zero members
/// — including its own creator — and nobody could ever add anyone to it.
/// </summary>
/// <param name="CallerUserId">
/// The Id of the authenticated user creating the project — resolved by the
/// API layer from the caller's validated principal, not supplied by the
/// caller as an arbitrary value.
/// </param>
/// <param name="Name">The project's name.</param>
public sealed record CreateProjectCommand(Guid CallerUserId, string Name)
    : IMediatRCommandRequest<Result<ProjectDto>>;
