using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Infrastructure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// Exposes non-sensitive deployment-mode config (ADR-0018) — currently
/// just whether this deployment is self-hosted, so the User Management
/// tab can hide "New User" for external-Authority deployments instead of
/// only failing after the form is filled out. Any authenticated user, not
/// architect-only: this reveals nothing about any account, only which
/// identity provider the deployment as a whole uses.
/// </summary>
[Route("api/auth")]
public sealed class AuthController(
    IOptions<AuthenticationConfiguration> authOptions, IUserProvisioningService userProvisioningService)
    : ApiControllerBase(userProvisioningService)
{
    [HttpGet("config")]
    public async Task<IActionResult> GetConfig(CancellationToken cancellationToken)
    {
        var callerUserId = await ResolveCallerUserIdAsync(cancellationToken);
        if (callerUserId is null)
        {
            return Unauthorized();
        }

        return Ok(new { useSelfHostedProvider = authOptions.Value.UseSelfHostedProvider });
    }
}
