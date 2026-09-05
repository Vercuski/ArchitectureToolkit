using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Projects.Commands;
using ArchitectureToolkit.Application.Actions.Projects.Queries;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

[Route("api/projects")]
public sealed class ProjectsController(IMediator mediator, IUserProvisioningService userProvisioningService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpPost]
    public async Task<IActionResult> CreateProject(
        [FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateProjectCommand(callerUserId.Value, request.Name), cancellationToken);

        return ToActionResult(result, project =>
            CreatedAtAction(nameof(GetProject), new { id = project.Id }, project));
    }

    [HttpGet]
    public async Task<IActionResult> ListProjects(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListProjectsQuery(callerUserId.Value), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProject(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new GetProjectQuery(callerUserId.Value, id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> ListProjectMembers(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListProjectMembersQuery(callerUserId.Value, id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> AddProjectMember(
        Guid id, [FromBody] AddProjectMemberRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new AddProjectMemberCommand(callerUserId.Value, id, request.UserId, request.Role), cancellationToken);

        return ToActionResult(result, member =>
            CreatedAtAction(nameof(ListProjectMembers), new { id }, member));
    }

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> UpdateProjectMemberRole(
        Guid id, Guid userId, [FromBody] UpdateProjectMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new UpdateProjectMemberRoleCommand(callerUserId.Value, id, userId, request.Role), cancellationToken);

        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveProjectMember(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new RemoveProjectMemberCommand(callerUserId.Value, id, userId), cancellationToken);

        return ToActionResult(result, _ => NoContent());
    }

    /// <summary>
    /// Streams a zip containing master.pdf (cover/TOC/contributors/links)
    /// plus one PDF per current document revision under documents/.
    /// </summary>
    [HttpGet("{id:guid}/export")]
    public async Task<IActionResult> ExportProject(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ExportProjectQuery(callerUserId.Value, id), cancellationToken);

        return ToActionResult(result, archive => File(archive.Content, "application/zip", archive.FileName));
    }
}
