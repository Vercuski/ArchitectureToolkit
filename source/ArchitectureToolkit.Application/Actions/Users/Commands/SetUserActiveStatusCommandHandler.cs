using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Users;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users.Commands;

public sealed class SetUserActiveStatusCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<SetUserActiveStatusCommand, Result<UserManagementDto>>
{
    public async Task<Result<UserManagementDto>> Handle(
        SetUserActiveStatusCommand request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<UserManagementDto>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        if (caller.SystemRole != SystemRole.Architect)
        {
            return Result<UserManagementDto>.Failure(
                "Only an architect may change a user's active status.", ResultErrorType.Forbidden);
        }

        // Read via queryDbContext then commandDbContext.Alter() below —
        // matches PromoteUserCommandHandler exactly. User isn't
        // xmin-tracked (unlike Template/ProjectDocument), so
        // ICommandDbContext.FindAsync's read-through-the-command-context
        // requirement doesn't apply here; using it anyway would just be an
        // inconsistency with every other User-modifying handler for no
        // benefit.
        var targetQuery = queryDbContext.Set<User>().Where(u => u.Id == request.TargetUserId);
        var target = await queryDbContext.SingleOrDefaultAsync(targetQuery, cancellationToken);

        if (target is null)
        {
            return Result<UserManagementDto>.Failure("Target user not found.", ResultErrorType.NotFound);
        }

        var isDeactivating = target.IsActive && !request.IsActive;

        if (isDeactivating)
        {
            var wouldRemoveLastActiveArchitect =
                await ActiveArchitectGuard.WouldRemoveLastActiveArchitectAsync(
                    queryDbContext, target, cancellationToken);

            if (wouldRemoveLastActiveArchitect)
            {
                return Result<UserManagementDto>.Failure(
                    "Cannot deactivate the last remaining active architect — " +
                    "promote or reactivate another user first.",
                    ResultErrorType.Conflict);
            }
        }

        target.SetActiveStatus(request.IsActive);
        commandDbContext.Alter(target);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserManagementDto>.Success(new UserManagementDto(target.Id, target.Email, target.IsActive));
    }
}
