using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

public sealed class ListProjectsQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListProjectsQuery, Result<IReadOnlyCollection<ProjectDto>>>
{
    public async Task<Result<IReadOnlyCollection<ProjectDto>>> Handle(
        ListProjectsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from member in queryDbContext.Set<ProjectMember>()
            where member.UserId == request.CallerUserId
            join project in queryDbContext.Set<Project>() on member.ProjectId equals project.Id
            select project;

        var projects = await queryDbContext.ToListAsync(query, cancellationToken);

        return Result<IReadOnlyCollection<ProjectDto>>.Success(
            projects.Select(p => new ProjectDto(p.Id, p.Name)).ToList());
    }
}
