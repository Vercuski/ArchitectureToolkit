using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Users;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users.Commands;

public sealed class PromoteUserCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<PromoteUserCommand, Result<SystemRole>>
{
    public async Task<Result<SystemRole>> Handle(PromoteUserCommand request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<SystemRole>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        if (caller.SystemRole != SystemRole.Architect)
        {
            return Result<SystemRole>.Failure(
                "Only an architect may promote or demote users.", ResultErrorType.Forbidden);
        }

        var targetQuery = queryDbContext.Set<User>().Where(u => u.Id == request.TargetUserId);
        var target = await queryDbContext.SingleOrDefaultAsync(targetQuery, cancellationToken);

        if (target is null)
        {
            return Result<SystemRole>.Failure("Target user not found.", ResultErrorType.NotFound);
        }

        var isDemotingAnArchitect = target.SystemRole == SystemRole.Architect
            && request.NewSystemRole != SystemRole.Architect;

        if (isDemotingAnArchitect)
        {
            // Not explicitly required by ADR-0009, which only specifies the
            // architect-only authorization check above — this is an
            // additional safety guard: without it, an architect could
            // demote themselves (or the only other architect) and leave no
            // one able to ever promote anyone again, since bootstrap
            // (ADR-0009/0014) only triggers on an empty USER table, not a
            // zero-architect one.
            //
            // ADR-0017: counts only *active* architects, via the guard
            // shared with SetUserActiveStatusCommandHandler. An architect
            // who is already deactivated doesn't count toward "someone can
            // still administer this install" in the first place, so
            // demoting them is never blocked here regardless of how many
            // other (possibly also inactive) architects exist.
            var wouldRemoveLastActiveArchitect =
                await ActiveArchitectGuard.WouldRemoveLastActiveArchitectAsync(
                    queryDbContext, target, cancellationToken);

            if (wouldRemoveLastActiveArchitect)
            {
                return Result<SystemRole>.Failure(
                    "Cannot demote the last remaining active architect — promote another active user first.",
                    ResultErrorType.Conflict);
            }
        }

        target.SetSystemRole(request.NewSystemRole);
        commandDbContext.Alter(target);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SystemRole>.Success(target.SystemRole);
    }
}
