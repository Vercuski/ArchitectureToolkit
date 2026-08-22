using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// Join entity granting a User a role on a Project. Unlike every other entity
/// in this model, PROJECT_MEMBER has no separate synthetic Id in the ERD — its
/// primary key is the (ProjectId, UserId) composite, so this deliberately does
/// not inherit from Entity/IEntity.
/// </summary>
public sealed class ProjectMember
{
    public Guid ProjectId { get; private set; }
    public Guid UserId { get; private set; }
    public ProjectRole Role { get; private set; }

    public ProjectMember(Guid projectId, Guid userId, ProjectRole role)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("ProjectId is required.", nameof(projectId));
        }
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        ProjectId = projectId;
        UserId = userId;
        Role = role;
    }
}
