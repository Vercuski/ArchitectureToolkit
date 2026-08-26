using ArchitectureToolkit.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// Shared base for controllers dispatching CQRS commands/queries via
/// MediatR. Centralizes two things every business controller needs:
///
/// 1. Resolving the caller's domain USER.Id from the authenticated
///    request's validated ClaimsPrincipal, via IUserProvisioningService
///    (ADR-0003/ADR-0004). This is the piece that was designed in Phase 3
///    (see PromoteUserCommand's doc comment: "resolved by the API layer
///    from the caller's validated principal") but never actually wired
///    into the request pipeline until now — nothing previously called
///    IUserProvisioningService outside its own definition and tests.
/// 2. Mapping a handler's Result&lt;T&gt; to the corresponding HTTP status
///    code, so that mapping lives in exactly one place rather than being
///    reimplemented per action.
/// </summary>
[ApiController]
[Authorize]
public abstract class ApiControllerBase(IUserProvisioningService userProvisioningService) : ControllerBase
{
    /// <summary>
    /// Resolves the authenticated caller's domain USER.Id. Returns null
    /// only in the defensive case where the validated principal is somehow
    /// missing the issuer/subject claims IUserProvisioningService requires
    /// — see that interface's doc comment; a genuinely validated token
    /// should never hit this path, but [Authorize] alone doesn't guarantee
    /// it, so callers must still check for null.
    /// </summary>
    protected async Task<Guid?> ResolveCallerUserIdAsync(CancellationToken cancellationToken)
    {
        var result = await userProvisioningService.ResolveOrProvisionUserAsync(User, cancellationToken);
        return result.IsSuccess ? result.Value!.Id : null;
    }

    /// <summary>
    /// Maps a Result&lt;T&gt; to the corresponding HTTP response.
    /// </summary>
    /// <param name="result">The handler's result.</param>
    /// <param name="onSuccess">
    /// How to shape the success response — e.g. Ok(value) for a query,
    /// CreatedAtAction(...) for a creation command. Defaults to Ok(value).
    /// </param>
    protected IActionResult ToActionResult<T>(Result<T> result, Func<T, IActionResult>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess is not null ? onSuccess(result.Value!) : Ok(result.Value);
        }

        var problem = new { error = result.Error };

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(problem),
            ResultErrorType.Validation => BadRequest(problem),
            ResultErrorType.Conflict => Conflict(problem),
            ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, problem),
            _ => StatusCode(StatusCodes.Status500InternalServerError, problem)
        };
    }
}
