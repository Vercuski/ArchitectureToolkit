using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Users;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users.Queries;

public sealed class ListUsersQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListUsersQuery, Result<IReadOnlyCollection<UserManagementDto>>>
{
    public async Task<Result<IReadOnlyCollection<UserManagementDto>>> Handle(
        ListUsersQuery request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<IReadOnlyCollection<UserManagementDto>>.Failure(
                "Caller not found.", ResultErrorType.NotFound);
        }

        if (caller.SystemRole != SystemRole.Architect)
        {
            return Result<IReadOnlyCollection<UserManagementDto>>.Failure(
                "Only an architect may view the user list.", ResultErrorType.Forbidden);
        }

        // Sorted server-side (ordering isn't guaranteed to survive a
        // round trip through JSON + the frontend's own rendering, so this
        // is the single source of truth for "alphabetical by email" the
        // User Management tab relies on) — same reasoning as
        // ListProjectMembersQueryHandler doing its own join in memory
        // rather than assuming client-side sorting.
        var usersQuery = queryDbContext.Set<User>().OrderBy(u => u.Email);
        var users = await queryDbContext.ToListAsync(usersQuery, cancellationToken);

        return Result<IReadOnlyCollection<UserManagementDto>>.Success(
            [.. users.Select(u => new UserManagementDto(u.Id, u.Email, u.IsActive))]);
    }
}
