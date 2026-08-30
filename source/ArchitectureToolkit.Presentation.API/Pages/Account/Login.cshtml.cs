using ArchitectureToolkit.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArchitectureToolkit.Presentation.API.Pages.Account;

/// <summary>
/// Minimal, intentionally unstyled credential-entry page for the
/// self-hosted OpenIddict server's login step. AuthorizationController
/// redirects here when /connect/authorize finds no active Identity
/// session; on success, the browser is sent back to that same
/// /connect/authorize request via <see cref="ReturnUrl"/> to complete the
/// authorization_code flow. Superseded by the Vue SPA once ADR-0005 lands
/// — this exists only so the flow has somewhere to send the browser today.
/// </summary>
public class LoginModel(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Nobody could possibly have credentials to enter yet — send the
        // browser to registration instead, which becomes the de facto
        // landing page the very first time the app is opened.
        if (!await IdentityBootstrapper.HasAnyIdentityUsersAsync(userManager))
        {
            return RedirectToPage("/Account/Register", new { ReturnUrl });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await signInManager.PasswordSignInAsync(
            Email, Password, isPersistent: true, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        return LocalRedirect(!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : "/");
    }
}
