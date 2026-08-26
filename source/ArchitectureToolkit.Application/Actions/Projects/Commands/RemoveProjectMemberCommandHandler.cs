using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

public sealed class RemoveProjectMemberCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<RemoveProjectMemberCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(RemoveProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            return Result<bool>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        if (callerMembership.Role != ProjectRole.Owner)
        {
            return Result<bool>.Failure("Only a project Owner may remove members.", ResultErrorType.Forbidden);
        }

        var targetMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.TargetUserId);
        var targetMembership = await queryDbContext.SingleOrDefaultAsync(targetMembershipQuery, cancellationToken);

        if (targetMembership is null)
        {
            return Result<bool>.Failure("Target is not a member of this project.", ResultErrorType.NotFound);
        }

        if (targetMembership.Role == ProjectRole.Owner)
        {
            var ownersQuery = queryDbContext.Set<ProjectMember>()
                .Where(pm => pm.ProjectId == request.ProjectId && pm.Role == ProjectRole.Owner);
            var owners = await queryDbContext.ToListAsync(ownersQuery, cancellationToken);

            if (owners.Count <= 1)
            {
                return Result<bool>.Failure(
                    "Cannot remove the last remaining Owner — promote another member first, " +
                    "or delete the project instead.",
                    ResultErrorType.Conflict);
            }
        }

        commandDbContext.Delete(targetMembership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
