using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Categories.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

[Route("api/categories")]
public sealed class CategoriesController(IMediator mediator, IUserProvisioningService userProvisioningService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpGet]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListCategoriesQuery(), cancellationToken);
        return ToActionResult(result);
    }
}
