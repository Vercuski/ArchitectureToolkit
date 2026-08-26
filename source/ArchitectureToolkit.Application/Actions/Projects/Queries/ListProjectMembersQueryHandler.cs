using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

public sealed class ListProjectMembersQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListProjectMembersQuery, Result<IReadOnlyCollection<ProjectMemberDto>>>
{
    public async Task<Result<IReadOnlyCollection<ProjectMemberDto>>> Handle(
        ListProjectMembersQuery request, CancellationToken cancellationToken)
    {
        var callerMembershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var callerMembership = await queryDbContext.SingleOrDefaultAsync(callerMembershipQuery, cancellationToken);

        if (callerMembership is null)
        {
            return Result<IReadOnlyCollection<ProjectMemberDto>>.Failure(
                "Project not found.", ResultErrorType.NotFound);
        }

        var membersQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId);
        var members = await queryDbContext.ToListAsync(membersQuery, cancellationToken);

        // Two round trips rather than one SQL JOIN: ToListAsync<TEntity> is
        // constrained to IPersistable, so an anonymous-type join projection
        // can't flow through it directly. Joining in memory via this
        // dictionary keeps every query entity-shaped instead.
        var userIds = members.Select(m => m.UserId).ToList();
        var usersQuery = queryDbContext.Set<User>().Where(u => userIds.Contains(u.Id));
        var users = await queryDbContext.ToListAsync(usersQuery, cancellationToken);
        var usersById = users.ToDictionary(u => u.Id);

        var memberDtos = members
            .Select(m => new ProjectMemberDto(
                m.ProjectId, m.UserId, usersById[m.UserId].Name, usersById[m.UserId].Email, m.Role))
            .ToList();

        return Result<IReadOnlyCollection<ProjectMemberDto>>.Success(memberDtos);
    }
}
