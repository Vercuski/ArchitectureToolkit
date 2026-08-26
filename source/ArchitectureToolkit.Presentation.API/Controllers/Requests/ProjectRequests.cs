using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

public sealed record CreateProjectRequest(string Name);

public sealed record AddProjectMemberRequest(Guid UserId, ProjectRole Role);

public sealed record UpdateProjectMemberRoleRequest(ProjectRole Role);
