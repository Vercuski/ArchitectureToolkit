using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Users;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Users.Queries;

public sealed class GetCurrentUserQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var query = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var user = await queryDbContext.SingleOrDefaultAsync(query, cancellationToken);

        if (user is null)
        {
            // Not reachable in practice: ApiControllerBase only resolves
            // CallerUserId via IUserProvisioningService, which JIT-provisions
            // the row if it doesn't already exist — a defensive guard only.
            return Result<UserDto>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        return Result<UserDto>.Success(new UserDto(user.Id, user.Name, user.Email, user.SystemRole.ToString()));
    }
}
