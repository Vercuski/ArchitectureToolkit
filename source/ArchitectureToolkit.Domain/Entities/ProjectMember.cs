using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// Join entity granting a User a role on a Project. Unlike every other entity
/// in this model, PROJECT_MEMBER has no separate synthetic Id in the ERD — its
/// primary key is the (ProjectId, UserId) composite, so this deliberately does
/// not inherit from Entity/IEntity. It implements IPersistable directly
/// instead, so it can still flow through ICommandDbContext/IQueryDbContext
/// like every Entity subtype does.
/// </summary>
public sealed class ProjectMember : IPersistable
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

    /// <summary>
    /// Changes this member's role on the project. Used by
    /// UpdateProjectMemberRoleCommand, which enforces (at the Application
    /// layer, not here) that only an existing Owner may call it, and that
    /// the last remaining Owner can't be demoted — mirroring the equivalent
    /// guards in PromoteUserCommandHandler for SystemRole.
    /// </summary>
    public void ChangeRole(ProjectRole newRole)
    {
        Role = newRole;
    }
}
