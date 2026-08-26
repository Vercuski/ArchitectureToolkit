using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Contracts.Projects;

public sealed record ProjectMemberDto(Guid ProjectId, Guid UserId, string UserName, string UserEmail, ProjectRole Role);
