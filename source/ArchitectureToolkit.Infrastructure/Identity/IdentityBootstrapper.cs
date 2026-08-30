using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ArchitectureToolkit.Infrastructure.Identity;

/// <summary>
/// Seeds the two things the self-hosted OpenIddict provider needs to be
/// usable at all on a fresh install, run once at startup alongside the
/// EF Core migrations (see Program.cs):
///
/// 1. The SPA's OAuth client (an OpenIddict "Application") — without this,
///    /connect/authorize rejects every request with "unknown client".
/// 2. The standard OIDC scopes the SPA requests. Unlike a custom
///    resource-audience scope (architecturetoolkit-api, validated purely
///    against the client's own Permissions), OpenIddict requires
///    well-known OIDC scopes like "email" to exist as a registered
///    OpenIddictScope entity — without this, /connect/authorize rejects
///    the request outright with "invalid scopes were specified: email",
///    even though the client already has permission to request it.
///
/// The very first Identity login itself is no longer seeded here — see
/// Pages/Account/Register.cshtml.cs, which lets whoever opens the app
/// first create it themselves rather than requiring a config-set
/// SeedAdminEmail/SeedAdminPassword before the app is usable at all.
/// <see cref="HasAnyIdentityUsersAsync"/> is the single shared check both
/// that page and Login.cshtml.cs use to decide which one to show.
///
/// Distinct from ADR-0009's still-open question of which domain USER gets
/// the Architect role — that's decided the first time whichever Identity
/// login gets created actually authenticates and IUserProvisioningService
/// (Persistence) JIT-provisions the corresponding USER row.
/// </summary>
public static class IdentityBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services, AuthenticationConfiguration config)
    {
        await SeedScopesAsync(services);
        await SeedOAuthClientAsync(services, config);
    }

    /// <summary>
    /// True when no Identity login exists yet — the single source of truth
    /// Login.cshtml.cs and Register.cshtml.cs both check to decide which
    /// page to show. Uses AnyAsync rather than the synchronous
    /// <c>Users.Any()</c> pattern acceptable in one-time startup seeding
    /// above: this runs on every request to those two pages, where a
    /// blocking sync-over-async call risks thread-pool starvation under
    /// real load.
    /// </summary>
    public static Task<bool> HasAnyIdentityUsersAsync(UserManager<IdentityUser> userManager)
    {
        return userManager.Users.AnyAsync();
    }

    private static async Task SeedScopesAsync(IServiceProvider services)
    {
        var scopeManager = services.GetRequiredService<IOpenIddictScopeManager>();

        if (await scopeManager.FindByNameAsync(Scopes.Email) is null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor { Name = Scopes.Email });
        }
    }

    private static async Task SeedOAuthClientAsync(IServiceProvider services, AuthenticationConfiguration config)
    {
        var applicationManager = services.GetRequiredService<IOpenIddictApplicationManager>();

        var existingApplication = await applicationManager.FindByClientIdAsync(config.ClientId);

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = config.ClientId,
            ClientType = ClientTypes.Public,
            // First-party client we ourselves seed: skip an explicit
            // consent screen rather than asking a self-hoster to approve
            // their own application's access to its own API.
            ConsentType = ConsentTypes.Implicit,
            DisplayName = "ArchitectureToolkit SPA",
        };

        foreach (var uri in config.RedirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        foreach (var uri in config.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);
        descriptor.Permissions.Add(Permissions.Prefixes.Scope + config.Audience);

        descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

        // Reconciling, not create-once: without this, any future change to
        // the permission/redirect-URI set above would silently never apply
        // to an already-seeded install, exactly as happened here —
        // Permissions.Prefixes.Scope + config.Audience was added to this
        // descriptor after this project's local database already had an
        // architecturetoolkit-spa row from an earlier iteration, and the
        // old "if exists, return" guard meant that row's permissions never
        // caught up, so OpenIddict rejected the architecturetoolkit-api
        // scope as one the (stale) client wasn't permitted to request.
        if (existingApplication is null)
        {
            await applicationManager.CreateAsync(descriptor);
        }
        else
        {
            await applicationManager.UpdateAsync(existingApplication, descriptor);
        }
    }
}
