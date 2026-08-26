using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Projects;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Projects.Commands;

public sealed class CreateProjectCommandHandler(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork)
    : IMediatRCommandHandler<CreateProjectCommand, Result<ProjectDto>>
{
    public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var callerQuery = queryDbContext.Set<User>().Where(u => u.Id == request.CallerUserId);
        var caller = await queryDbContext.SingleOrDefaultAsync(callerQuery, cancellationToken);

        if (caller is null)
        {
            return Result<ProjectDto>.Failure("Caller not found.", ResultErrorType.NotFound);
        }

        Project project;
        try
        {
            project = new Project(request.Name);
        }
        catch (ArgumentException ex)
        {
            // Domain constructors validate via ArgumentException, which is
            // correct for a programming-error-style guard clause — but at
            // this API-facing boundary, bad input from an external caller
            // (an empty Name) is a 400 Validation failure, not a 500.
            return Result<ProjectDto>.Failure(ex.Message, ResultErrorType.Validation);
        }

        var ownerMembership = new ProjectMember(project.Id, caller.Id, ProjectRole.Owner);

        commandDbContext.Insert(project);
        commandDbContext.Insert(ownerMembership);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ProjectDto>.Success(new ProjectDto(project.Id, project.Name));
    }
}
