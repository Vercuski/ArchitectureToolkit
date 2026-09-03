using ArchitectureToolkit.Infrastructure.Setup;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using ArchitectureToolkit.Presentation.API.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// First-run setup (see the "Removing appsettings.json secrets" ADR).
///
/// Deliberately does NOT inherit ApiControllerBase: that base type
/// requires IUserProvisioningService and enforces [Authorize], neither of
/// which exist in this process at all while unconfigured — no DbContext,
/// no Identity, no OpenIddict, no authentication scheme is registered in
/// Setup Mode (Program.cs). Both actions are [AllowAnonymous] for the
/// same reason, not because they're meant to stay reachable once actually
/// configured — GetStatus reveals nothing sensitive either way, and
/// Complete re-checks whether setup has already run so a request racing
/// (or following) a completed setup can't run again.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/setup")]
public sealed class SetupController(
    IAppConfigurationStore configurationStore,
    SetupCompletionService completionService,
    SetupState setupState) : ControllerBase
{
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { isConfigured = setupState.IsConfigured });
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete(CompleteSetupRequest request, CancellationToken cancellationToken)
    {
        if (configurationStore.IsConfigured)
        {
            // Already configured (e.g. a stale setup tab resubmitted
            // after another request completed it first) — nothing to do,
            // and re-running would silently overwrite the first request's
            // still-pending PendingInitialUser before the next boot ever
            // gets to consume it.
            return Conflict(new { error = "Setup has already been completed." });
        }

        var result = await completionService.CompleteAsync(request, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok();
    }
}
