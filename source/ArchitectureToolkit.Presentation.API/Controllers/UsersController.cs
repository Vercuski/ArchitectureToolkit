using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Users.Commands;
using ArchitectureToolkit.Application.Actions.Users.Queries;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
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

    /// <summary>
    /// User Management tab (ADR-0017) — architect-only, enforced in
    /// ListUsersQueryHandler, not here (Forbidden surfaces as a normal
    /// Result failure through ToActionResult, same as every other
    /// architect-only action on this project).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new ListUsersQuery(callerUserId.Value), cancellationToken);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetUserActiveStatus(
        Guid id, [FromBody] SetUserActiveStatusRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new SetUserActiveStatusCommand(callerUserId.Value, id, request.IsActive), cancellationToken);

        return ToActionResult(result);
    }

    /// <summary>
    /// Admin-provisions a new user (ADR-0018) — architect-only,
    /// self-hosted deployments only (CreateUserCommandHandler returns
    /// Conflict otherwise). See GET api/auth/config for how the frontend
    /// knows to hide this action entirely under an external Authority.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(
            new CreateUserCommand(callerUserId.Value, request.Email, request.SystemRole), cancellationToken);

        return ToActionResult(result);
    }
}
