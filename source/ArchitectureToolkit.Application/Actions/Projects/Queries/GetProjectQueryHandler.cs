using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

public sealed class GetProjectQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetProjectQuery, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
    {
        var projectQuery = queryDbContext.Set<Project>().Where(p => p.Id == request.ProjectId);
        var project = await queryDbContext.SingleOrDefaultAsync(projectQuery, cancellationToken);

        if (project is null)
        {
            return Result<ProjectDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        var membershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var membership = await queryDbContext.SingleOrDefaultAsync(membershipQuery, cancellationToken);

        if (membership is null)
        {
            // NotFound, not Forbidden: a non-member shouldn't be able to
            // confirm a project exists just by probing its Id.
            return Result<ProjectDto>.Failure("Project not found.", ResultErrorType.NotFound);
        }

        return Result<ProjectDto>.Success(new ProjectDto(project.Id, project.Name));
    }
}
