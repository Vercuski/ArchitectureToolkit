using ArchitectureToolkit.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ArchitectureToolkit.Presentation.API.Controllers;

/// <summary>
/// Implements the interactive parts of ADR-0003's self-hosted OpenIddict
/// server: /connect/authorize (redirect to login if no session; auto-issue
/// for our seeded first-party client, per its Implicit ConsentType) and
/// /connect/token (completes the authorization_code and refresh_token
/// grants). Registered because
/// DependencyInjection.AddIdentityAuthenticationRegistration enables
/// EnableAuthorizationEndpointPassthrough/EnableTokenEndpointPassthrough,
/// which hands actual request handling to application code rather than
/// OpenIddict itself — this controller is that application code, following
/// OpenIddict's standard integration pattern.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class AuthorizationController(
    SignInManager<IdentityUser> signInManager,
    IOpenIddictApplicationManager applicationManager,
    IOptions<AuthenticationConfiguration> authConfig) : ControllerBase
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request is missing.");

        var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!authenticateResult.Succeeded || authenticateResult.Principal.Identity is not { IsAuthenticated: true })
        {
            return ChallengeToLogin();
        }

        // Exists so an unknown/removed client fails clearly here rather
        // than surfacing as an opaque error deeper in the token pipeline.
        _ = await applicationManager.FindByClientIdAsync(request.ClientId ?? string.Empty)
            ?? throw new InvalidOperationException("The specified client is unknown.");

        // The Identity cookie authenticated successfully, but the account
        // it points at may no longer be resolvable — e.g. the user row was
        // deleted, or the database was reset/restored, after the cookie
        // was issued. Previously this fell through to an unhandled
        // InvalidOperationException — an unstyled 500 the browser had no
        // way to recover from. Instead, treat it the same as "no session
        // at all": clear the stale cookie and send the browser back to the
        // login page with an explanatory message, so the person can simply
        // sign in again rather than being stuck on a crashed request.
        var userId = signInManager.UserManager.GetUserId(authenticateResult.Principal);
        var user = userId is null ? null : await signInManager.UserManager.FindByIdAsync(userId);
        if (user is null)
        {
            await signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login", new
            {
                ReturnUrl = BuildAuthorizeReturnUrl(),
                Error = "Your session is no longer valid. Please sign in again.",
            });
        }

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id);
        identity.SetClaim(Claims.Email, user.Email);
        identity.SetClaim(Claims.Name, user.UserName);

        identity.SetScopes(request.GetScopes());
        identity.SetResources(identity.GetScopes().Contains(authConfig.Value.Audience)
            ? [authConfig.Value.Audience]
            : []);

        foreach (var claim in identity.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, identity));
        }

        return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request is missing.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // The identity produced by Authorize() above (auth code grant)
            // or by a prior Exchange() call (refresh token grant) was
            // persisted by OpenIddict itself when SignIn() ran; retrieving
            // it here via the OpenIddict validation scheme is what lets a
            // refresh token mint a new access token without the user
            // re-authenticating.
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            if (!result.Succeeded || result.Principal is null)
            {
                return Forbid(
                    authenticationSchemes: [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme],
                    properties: new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The token is no longer valid."
                    }));
            }

            return SignIn(result.Principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new NotImplementedException(
            "Only the authorization_code and refresh_token grant types are supported.");
    }

    /// <summary>
    /// RP-initiated logout (OpenID Connect Session Management). Ends the
    /// local Identity cookie session, then hands off to OpenIddict's own
    /// SignOut handling, which validates the request's
    /// post_logout_redirect_uri against the client's registered
    /// PostLogoutRedirectUris and redirects the browser there — this
    /// controller doesn't need to read or validate that URI itself.
    /// </summary>
    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Standard OpenIddict claim-destination mapping: everything goes into
    /// the access token (so the API can read it); name/email additionally
    /// go into the ID token only when the corresponding scope was granted,
    /// so the SPA doesn't receive claims it didn't ask for.
    /// </summary>
    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity)
    {
        switch (claim.Type)
        {
            case Claims.Name or Claims.Email:
                yield return Destinations.AccessToken;
                if (identity.HasScope(Scopes.Profile) || identity.HasScope(Scopes.Email))
                {
                    yield return Destinations.IdentityToken;
                }
                break;

            case Claims.Subject:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            default:
                yield return Destinations.AccessToken;
                break;
        }
    }

    /// <summary>
    /// Challenges the Identity cookie scheme, sending the browser to the
    /// login page with a ReturnUrl that comes straight back to this exact
    /// /connect/authorize request (same query string/form body) once
    /// authenticated.
    /// </summary>
    private IActionResult ChallengeToLogin() =>
        Challenge(
            authenticationSchemes: [IdentityConstants.ApplicationScheme],
            properties: new AuthenticationProperties { RedirectUri = BuildAuthorizeReturnUrl() });

    private string BuildAuthorizeReturnUrl() =>
        Request.PathBase + Request.Path + QueryString.Create(
            Request.HasFormContentType ? [.. Request.Form] : Request.Query.ToList());
}
