using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

public sealed class AddProjectMemberCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<AddProjectMemberCommand, Result<ProjectMemberDto>>
{
    public async Task<Result<ProjectMemberDto>> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var projectQuery = queryDbContext.Set<Project>().Where(p => p.Id == request.ProjectId);
        var project = await queryDbContext.SingleOrDefaultAsync(projectQuery, cancellationToken);

        if (project is null)
        {
            return Result<ProjectMemberDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            // NotFound, not Forbidden: a non-member shouldn't be able to
            // confirm a project exists just by probing its Id.
            return Result<ProjectMemberDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        if (callerMembership.Role != ProjectRole.Owner)
        {
            return Result<ProjectMemberDto>.Failure(
                "Only a project Owner may add members.", ResultErrorType.Forbidden);
        }

        var targetQuery = queryDbContext.Set<User>().Where(u => u.Id == request.TargetUserId);
        var target = await queryDbContext.SingleOrDefaultAsync(targetQuery, cancellationToken);

        if (target is null)
        {
            return Result<ProjectMemberDto>.Failure("Target user not found.", ResultErrorType.NotFound);
        }

        var existingMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.TargetUserId);
        var existingMembership = await queryDbContext.SingleOrDefaultAsync(existingMembershipQuery, cancellationToken);

        if (existingMembership is not null)
        {
            return Result<ProjectMemberDto>.Failure(
                "That user is already a member of this project.", ResultErrorType.Conflict);
        }

        var newMembership = new ProjectMember(request.ProjectId, request.TargetUserId, request.Role);
        commandDbContext.Insert(newMembership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectMemberDto>.Success(
            new ProjectMemberDto(request.ProjectId, target.Id, target.Name, target.Email, newMembership.Role));
    }
}
