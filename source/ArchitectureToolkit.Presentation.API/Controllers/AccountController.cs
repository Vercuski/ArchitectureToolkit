using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// Deliberately not ApiControllerBase-derived (that base class carries
/// [Authorize] at the class level) and deliberately not routed through
/// MediatR — this is a direct ASP.NET Core Identity operation, the same
/// shape Register.cshtml.cs already uses at the Presentation layer
/// (ADR-0018). Reachable before any session exists, since that's the
/// entire point: this is what the invite link itself points at.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/account")]
public sealed class AccountController(UserManager<IdentityUser> userManager) : ControllerBase
{
    [HttpPost("set-password")]
    public async Task<IActionResult> SetPassword([FromBody] SetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { error = "Password and confirmation do not match." });
        }

        var identityUser = await userManager.FindByEmailAsync(request.Email);
        if (identityUser is null)
        {
            // Same message regardless of "no such account" vs. "bad
            // token" below — doesn't confirm or deny which email
            // addresses have pending invites to an anonymous caller.
            return BadRequest(new { error = "This link is invalid or has expired." });
        }

        var result = await userManager.ResetPasswordAsync(identityUser, request.Token, request.NewPassword);
        if (!result.Succeeded)
        {
            var isTokenProblem = result.Errors.Any(e => e.Code.Contains("Token", StringComparison.OrdinalIgnoreCase));
            var error = isTokenProblem
                ? "This link is invalid or has expired."
                : string.Join(" ", result.Errors.Select(e => e.Description));

            return BadRequest(new { error });
        }

        return Ok();
    }
}
