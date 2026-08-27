using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

[Route("api/users")]
public sealed class UsersController(IMediator mediator, IUserProvisioningService userProvisioningService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new GetCurrentUserQuery(callerUserId.Value), cancellationToken);
        return ToActionResult(result);
    }
}
