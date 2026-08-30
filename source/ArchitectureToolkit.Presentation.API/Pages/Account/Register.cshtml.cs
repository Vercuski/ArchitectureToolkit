using ArchitectureToolkit.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ArchitectureToolkit.Presentation.API.Pages.Account;

/// <summary>
/// Replaces the old config-seeded bootstrap admin
/// (AuthenticationConfiguration.SeedAdminEmail/SeedAdminPassword,
/// removed) with self-service registration: whoever opens the app first,
/// on a genuinely empty install, creates their own Identity login here
/// instead of a self-hoster having to set credentials via configuration
/// before the app is usable at all.
///
/// Only reachable while <see cref="IdentityBootstrapper.HasAnyIdentityUsersAsync"/>
/// is false — both <see cref="OnGetAsync"/> and <see cref="OnPostAsync"/>
/// re-check this and bounce to Login otherwise, since registration must
/// close the moment a first account exists; nothing about this page's
/// intent is "invite more users," only "bootstrap the very first one."
/// The domain-layer effect (this login's USER row is auto-promoted to
/// Architect and the template library seeded) is unchanged — that's
/// ADR-0009/ADR-0014's IUserProvisioningService logic, triggered the same
/// way regardless of how the underlying Identity login was created.
/// </summary>
public class RegisterModel(
    SignInManager<IdentityUser> signInManager,
    UserManager<IdentityUser> userManager) : PageModel
{
    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await IdentityBootstrapper.HasAnyIdentityUsersAsync(userManager))
        {
            return RedirectToPage("/Account/Login", new { ReturnUrl });
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Re-checked here, not just on the GET that rendered this form:
        // without it, two browser tabs opened before anyone has registered
        // could both submit and create two "first" accounts, each
        // separately eligible for IUserProvisioningService's
        // empty-USER-table Architect promotion. A narrow race remains —
        // accepted for a self-hosted, typically single-operator install,
        // same reasoning ADR-0009 already applied to that promotion check.
        if (await IdentityBootstrapper.HasAnyIdentityUsersAsync(userManager))
        {
            return RedirectToPage("/Account/Login", new { ReturnUrl });
        }

        if (Password != ConfirmPassword)
        {
            ModelState.AddModelError(string.Empty, "Password and confirmation do not match.");
            return Page();
        }

        var user = new IdentityUser
        {
            UserName = Email,
            Email = Email,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        await signInManager.SignInAsync(user, isPersistent: true);

        return LocalRedirect(!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
            ? ReturnUrl
            : "/");
    }
}
