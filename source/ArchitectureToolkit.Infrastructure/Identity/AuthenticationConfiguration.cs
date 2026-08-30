namespace ArchitectureToolkit.Infrastructure.Identity;

/// <summary>
/// Bound from the "Authentication" configuration section (ADR-0003 §3/§5).
///
/// Left with a null/empty <see cref="Authority"/>, ArchitectureToolkit falls
/// back to its zero-dependency self-hosted default: an OpenIddict
/// authorization server backed by ASP.NET Core Identity, running in the
/// same process (<see cref="ApplicationIdentityDbContext"/>).
///
/// Setting <see cref="Authority"/> to an external provider's issuer URL
/// (Auth0, Keycloak, Microsoft Entra External ID, Okta, ...) swaps that
/// provider in instead — token *validation* always goes through the same
/// OpenIddict.Validation code path either way, so no code changes, only
/// configuration.
/// </summary>
public sealed class AuthenticationConfiguration
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// The external OIDC provider's issuer URL. Leave null/empty to use
    /// the built-in self-hosted OpenIddict server instead.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// The OAuth client ID the Vue SPA authenticates as. Only meaningful
    /// for the self-hosted default today; an external provider's own
    /// client registration is configured on that provider's side.
    /// </summary>
    public string ClientId { get; set; } = "architecturetoolkit-spa";

    /// <summary>
    /// The resource/audience value ArchitectureToolkit's API expects on
    /// incoming access tokens, and the scope the self-hosted server issues
    /// tokens for.
    /// </summary>
    public string Audience { get; set; } = "architecturetoolkit-api";

    /// <summary>
    /// True when no external <see cref="Authority"/> is configured, meaning
    /// ArchitectureToolkit should stand up its own self-hosted OpenIddict
    /// server (backed by ASP.NET Core Identity) rather than validating
    /// tokens against an external provider.
    /// </summary>
    public bool UseSelfHostedProvider => string.IsNullOrWhiteSpace(Authority);

    /// <summary>
    /// Redirect URIs the seeded SPA client (<see cref="ClientId"/>) is
    /// permitted to complete the authorization_code + PKCE flow against.
    /// Only meaningful for the self-hosted default. Defaults to the Vite
    /// dev server's standard port, per ADR-0005's local-dev topology
    /// (SPA and API are same-origin only in the production wwwroot build;
    /// during development the SPA runs as its own process/origin). A real
    /// deployment must override this with its actual origin once ADR-0005
    /// lands and that origin is known.
    /// </summary>
    public string[] RedirectUris { get; set; } = ["http://localhost:5173/auth/callback"];

    /// <summary>
    /// Where the browser is sent after a logout completes. Same
    /// dev-default caveat as <see cref="RedirectUris"/>.
    /// </summary>
    public string[] PostLogoutRedirectUris { get; set; } = ["http://localhost:5173/"];
}
