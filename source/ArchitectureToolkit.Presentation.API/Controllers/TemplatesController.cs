using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Templates.Commands;
using ArchitectureToolkit.Application.Actions.Templates.Queries;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

[Route("api/templates")]
public sealed class TemplatesController(IMediator mediator, IUserProvisioningService userProvisioningService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpGet]
    public async Task<IActionResult> ListTemplates(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListTemplatesQuery(), cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTemplate(Guid id, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new GetTemplateQuery(id), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateTemplateCommand(callerUserId.Value, request.CategoryId, request.Name, request.Content),
            cancellationToken);

        return ToActionResult(result, template =>
            CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, template));
    }

    [HttpPost("{id:guid}/revisions")]
    public async Task<IActionResult> CreateTemplateRevision(
        Guid id, [FromBody] CreateTemplateRevisionRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateTemplateRevisionCommand(
                callerUserId.Value, id, request.ExpectedCurrentRevisionId, request.BumpType, request.Content),
            cancellationToken);

        return ToActionResult(result, revision =>
            CreatedAtAction(nameof(GetTemplate), new { id }, revision));
    }
}
