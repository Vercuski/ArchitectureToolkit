using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ArchitectureToolkit.Infrastructure.Identity;

/// <summary>
/// Seeds the three things the self-hosted OpenIddict provider needs to be
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
/// 3. An optional bootstrap admin Identity login
///    (<see cref="AuthenticationConfiguration.SeedAdminEmail"/>/
///    <see cref="AuthenticationConfiguration.SeedAdminPassword"/>) —
///    without this, and with no self-registration UI built yet (ADR-0003
///    follow-up), nobody could ever sign in to create further Identity
///    users through the product itself.
///
/// Distinct from ADR-0009's still-open question of which domain USER gets
/// the Architect role — that's decided the first time this seeded login
/// actually authenticates and IUserProvisioningService (Persistence)
/// JIT-provisions the corresponding USER row.
/// </summary>
public static class IdentityBootstrapper
{
    public static async Task SeedAsync(IServiceProvider services, AuthenticationConfiguration config)
    {
        await SeedScopesAsync(services);
        await SeedOAuthClientAsync(services, config);
        await SeedAdminLoginAsync(services, config);
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

        if (await applicationManager.FindByClientIdAsync(config.ClientId) is not null)
        {
            return;
        }

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

        await applicationManager.CreateAsync(descriptor);
    }

    private static async Task SeedAdminLoginAsync(IServiceProvider services, AuthenticationConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.SeedAdminEmail) || string.IsNullOrWhiteSpace(config.SeedAdminPassword))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Only ever seeds the very first login on an empty install — never
        // overwrites or resets an existing admin's credentials on restart.
        if (userManager.Users.Any())
        {
            return;
        }

        var user = new IdentityUser
        {
            UserName = config.SeedAdminEmail,
            Email = config.SeedAdminEmail,
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(user, config.SeedAdminPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed the bootstrap admin login: {errors}");
        }
    }
}
