using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Actions.Users.Queries;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using ArchitectureToolkit.Presentation.API.Setup;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// Architect-only ongoing configuration (ADR-0024) — deliberately
/// narrower than the first-run Setup Wizard: only SMTP and the template
/// library root path are readable/editable here. Database connection
/// strings and the OAuth Authority/ClientId/Audience stay out of scope —
/// changing those while the app is live is a redeploy-level operation (a
/// new connection string means a running process needs to re-migrate and
/// potentially point at a completely different database; ADR-0023 also
/// moved RedirectUris out of the encrypted config entirely), not
/// something a "click save" settings form should invite. See ADR-0024
/// for the full reasoning and the fields deliberately left out.
///
/// Not routed through MediatR/CQRS like Users/Projects/Templates: reading
/// and writing settings needs IAppConfigurationStore, and Application has
/// no project reference to Infrastructure at all (the same constraint
/// SetupCompletionService's own doc comment describes for its own,
/// adjacent needs) — SettingsService lives directly in Presentation.API,
/// this project's established composition root for exactly this kind of
/// cross-layer wiring, and this controller calls it directly. The one
/// piece that legitimately does belong in Application — "is the caller an
/// architect" — is answered by reusing the existing GetCurrentUserQuery
/// rather than duplicating that lookup here.
/// </summary>
[Route("api/settings")]
public sealed class SettingsController(
    IMediator mediator, IUserProvisioningService userProvisioningService, SettingsService settingsService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpGet]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var authorizationResult = await RequireArchitectAsync(cancellationToken);
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        return Ok(settingsService.GetCurrent());
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        var authorizationResult = await RequireArchitectAsync(cancellationToken);
        if (authorizationResult is not null)
        {
            return authorizationResult;
        }

        var result = settingsService.Update(request);

        return result.Succeeded ? Ok() : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Null when the caller is a resolved, active architect and may
    /// proceed; otherwise the IActionResult the action should return
    /// directly. Settings has no MediatR command/query of its own to put
    /// this check inside (see this controller's own doc comment), so —
    /// uniquely among this project's Architect-gated actions — the check
    /// happens here rather than inside a handler, by reusing
    /// GetCurrentUserQuery rather than re-querying SystemRole directly.
    /// </summary>
    private async Task<IActionResult?> RequireArchitectAsync(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        var result = await mediator.Send(new GetCurrentUserQuery(callerUserId.Value), cancellationToken);
        if (!result.IsSuccess || result.Value!.SystemRole != "Architect")
        {
            return StatusCode(
                StatusCodes.Status403Forbidden, new { error = "Only an architect may view or change settings." });
        }

        return null;
    }
}
