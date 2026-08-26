using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

public sealed class UpdateProjectMemberRoleCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<UpdateProjectMemberRoleCommand, Result<ProjectMemberDto>>
{
    public async Task<Result<ProjectMemberDto>> Handle(
        UpdateProjectMemberRoleCommand request, CancellationToken cancellationToken)
    {
        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            return Result<ProjectMemberDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        if (callerMembership.Role != ProjectRole.Owner)
        {
            return Result<ProjectMemberDto>.Failure(
                "Only a project Owner may change member roles.", ResultErrorType.Forbidden);
        }

        var targetMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.TargetUserId);
        var targetMembership = await queryDbContext.SingleOrDefaultAsync(targetMembershipQuery, cancellationToken);

        if (targetMembership is null)
        {
            return Result<ProjectMemberDto>.Failure("Target is not a member of this project.", ResultErrorType.NotFound);
        }

        var isDemotingAnOwner = targetMembership.Role == ProjectRole.Owner
            && request.NewRole != ProjectRole.Owner;

        if (isDemotingAnOwner)
        {
            var ownersQuery = queryDbContext.Set<ProjectMember>()
                .Where(pm => pm.ProjectId == request.ProjectId && pm.Role == ProjectRole.Owner);
            var owners = await queryDbContext.ToListAsync(ownersQuery, cancellationToken);

            if (owners.Count <= 1)
            {
                return Result<ProjectMemberDto>.Failure(
                    "Cannot demote the last remaining Owner — promote another member first.",
                    ResultErrorType.Conflict);
            }
        }

        targetMembership.ChangeRole(request.NewRole);
        commandDbContext.Alter(targetMembership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var targetUserQuery = queryDbContext.Set<User>().Where(u => u.Id == request.TargetUserId);
        var targetUser = await queryDbContext.SingleOrDefaultAsync(targetUserQuery, cancellationToken);

        return Result<ProjectMemberDto>.Success(new ProjectMemberDto(
            request.ProjectId,
            request.TargetUserId,
            targetUser?.Name ?? string.Empty,
            targetUser?.Email ?? string.Empty,
            targetMembership.Role));
    }
}
