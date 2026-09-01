using ArchitectureToolkit.Infrastructure.Identity;
using ArchitectureToolkit.Presentation.API;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace ArchitectureToolkit.Tests.InfrastructureTests.Identity;

/// <summary>
/// Full authorization_code + PKCE + refresh_token round trip against the
/// real ASP.NET Core pipeline (login page, AuthorizationController,
/// OpenIddict server) — the automated version of the manual live
/// verification run during ADR-0003's OAuth follow-up work.
///
/// Deliberately excluded from the default test run: unlike the rest of the
/// suite (which is verified to pass with PostgreSQL fully stopped), this
/// test needs a real reachable database, since it exercises Identity's and
/// OpenIddict's actual EF Core stores end-to-end rather than mocking them.
/// Run explicitly with `dotnet test --filter Category=RequiresDatabase`;
/// the default `dotnet test` run (or an explicit
/// `--filter Category!=RequiresDatabase`) skips it, preserving the
/// established DB-independent baseline for everything else.
/// </summary>
[TestFixture]
[Category("RequiresDatabase")]
public class OAuthAuthorizationCodeFlowTests
{
    private const string Audience = "architecturetoolkit-api";
    private const string RedirectUri = "http://localhost/callback";

    [Test]
    public async Task AuthorizationCodeFlow_Should_IssueTokens_AndSupportRefresh()
    {
        var email = $"e2e-test-{Guid.NewGuid():N}@architecturetoolkit.local";
        var clientId = $"e2e-test-client-{Guid.NewGuid():N}";
        const string password = "Correct-Horse-Battery-Staple-9!";

        var factory = new WebApplicationFactory<ApiAssemblyMarker>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                "Authentication:RedirectUris:0", RedirectUri));

        // Testing environment intentionally skips both migrations and
        // seeding (see Program.cs) so unrelated tests never need a
        // database. This test needs both a client and a real user, so it
        // seeds them directly through the same DI-resolved services and
        // code path production uses (IdentityBootstrapper /
        // UserManager<IdentityUser>) rather than duplicating that logic.
        using (var scope = factory.Services.CreateScope())
        {
            var authConfig = new AuthenticationConfiguration
            {
                ClientId = clientId,
                Audience = Audience,
                RedirectUris = [RedirectUri],
            };
            await IdentityBootstrapper.SeedAsync(scope.ServiceProvider, authConfig);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user, password);
            Assert.That(createResult.Succeeded, Is.True,
                () => string.Join("; ", createResult.Errors.Select(e => e.Description)));
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var codeVerifier = GeneratePkceVerifier();
        var codeChallenge = ComputeS256Challenge(codeVerifier);
        var state = Guid.NewGuid().ToString("N");

        var authorizeUrl = "/connect/authorize" + QueryString(new()
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = $"openid offline_access {Audience}",
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["state"] = state,
        });

        // Step 1: unauthenticated -> redirected to the login page.
        var authorizeRequest = new HttpRequestMessage(HttpMethod.Get, authorizeUrl);
        authorizeRequest.Headers.Add("Accept", "text/html");
        var step1 = await client.SendAsync(authorizeRequest);
        Assert.That(step1.StatusCode, Is.EqualTo(HttpStatusCode.Found), "expected redirect to login");
        var loginUrl = step1.Headers.Location!.OriginalString;
        Assert.That(loginUrl, Does.Contain("/Account/Login"));

        // Step 2: GET the login page, extract the antiforgery token.
        var loginPageHtml = await client.GetStringAsync(loginUrl);
        var tokenMatch = Regex.Match(
            loginPageHtml, "name=\"__RequestVerificationToken\"[^>]*\\bvalue=\"([^\"]+)\"");
        Assert.That(tokenMatch.Success, Is.True, "antiforgery token not found in login page");
        var antiforgeryToken = tokenMatch.Groups[1].Value;

        var returnUrlMatch = Regex.Match(
            loginPageHtml, "name=\"ReturnUrl\"[^>]*\\bvalue=\"([^\"]*)\"");
        var returnUrl = WebUtility.HtmlDecode(returnUrlMatch.Groups[1].Value);

        // Step 3: submit credentials.
        var loginResponse = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["ReturnUrl"] = returnUrl,
                ["__RequestVerificationToken"] = antiforgeryToken,
            }));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Found), "expected redirect after login");

        // Step 4: follow back to /connect/authorize, now authenticated.
        var authorizeAgain = await client.GetAsync(loginResponse.Headers.Location);
        Assert.That(authorizeAgain.StatusCode, Is.EqualTo(HttpStatusCode.Found),
            "expected redirect with authorization code");
        var callbackUri = authorizeAgain.Headers.Location!;
        var callbackQuery = HttpUtility.ParseQueryString(callbackUri.Query);
        var code = callbackQuery["code"];
        using (Assert.EnterMultipleScope())
        {
            Assert.That(code, Is.Not.Null.And.Not.Empty, "no authorization code in redirect");
            Assert.That(callbackQuery["state"], Is.EqualTo(state));
        }

        // Step 5: exchange the code for tokens.
        var tokenResponse = await client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code!,
                ["redirect_uri"] = RedirectUri,
                ["client_id"] = clientId,
                ["code_verifier"] = codeVerifier,
            }));
        Assert.That(tokenResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            () => tokenResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());

        var tokens = JsonSerializer.Deserialize<JsonElement>(await tokenResponse.Content.ReadAsStringAsync());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(tokens.GetProperty("token_type").GetString(), Is.EqualTo("Bearer"));
            Assert.That(tokens.GetProperty("scope").GetString(), Does.Contain(Audience));
        }
        Assert.That(tokens.TryGetProperty("refresh_token", out var refreshTokenElement), Is.True,
            "expected a refresh_token since offline_access was requested");

        // Step 6: the refresh token actually mints a new access token.
        var refreshResponse = await client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshTokenElement.GetString()!,
                ["client_id"] = clientId,
            }));
        Assert.That(refreshResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
            () => refreshResponse.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }

    [Test]
    public async Task Authorize_Should_RedirectToLoginWithError_When_SignedInUsersAccountNoLongerExists()
    {
        var email = $"stale-user-{Guid.NewGuid():N}@architecturetoolkit.local";
        var clientId = $"stale-user-client-{Guid.NewGuid():N}";
        const string password = "Correct-Horse-Battery-Staple-9!";

        var factory = new WebApplicationFactory<ApiAssemblyMarker>()
            .WithWebHostBuilder(builder => builder.UseSetting(
                "Authentication:RedirectUris:0", RedirectUri));

        string identityUserId;
        using (var scope = factory.Services.CreateScope())
        {
            var authConfig = new AuthenticationConfiguration
            {
                ClientId = clientId,
                Audience = Audience,
                RedirectUris = [RedirectUri],
            };
            await IdentityBootstrapper.SeedAsync(scope.ServiceProvider, authConfig);

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            var createResult = await userManager.CreateAsync(user, password);
            Assert.That(createResult.Succeeded, Is.True,
                () => string.Join("; ", createResult.Errors.Select(e => e.Description)));
            identityUserId = user.Id;
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var authorizeUrl = "/connect/authorize" + QueryString(new()
        {
            ["client_id"] = clientId,
            ["redirect_uri"] = RedirectUri,
            ["response_type"] = "code",
            ["scope"] = $"openid offline_access {Audience}",
            ["code_challenge"] = ComputeS256Challenge(GeneratePkceVerifier()),
            ["code_challenge_method"] = "S256",
            ["state"] = Guid.NewGuid().ToString("N"),
        });

        // Same login-through-cookie sequence as the happy-path test above,
        // just to obtain a valid, signed-in HttpClient.
        var authorizeRequest = new HttpRequestMessage(HttpMethod.Get, authorizeUrl);
        authorizeRequest.Headers.Add("Accept", "text/html");
        var step1 = await client.SendAsync(authorizeRequest);
        var loginUrl = step1.Headers.Location!.OriginalString;

        var loginPageHtml = await client.GetStringAsync(loginUrl);
        var tokenMatch = Regex.Match(
            loginPageHtml, "name=\"__RequestVerificationToken\"[^>]*\\bvalue=\"([^\"]+)\"");
        var antiforgeryToken = tokenMatch.Groups[1].Value;
        var returnUrlMatch = Regex.Match(
            loginPageHtml, "name=\"ReturnUrl\"[^>]*\\bvalue=\"([^\"]*)\"");
        var returnUrl = WebUtility.HtmlDecode(returnUrlMatch.Groups[1].Value);

        var loginResponse = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = password,
                ["ReturnUrl"] = returnUrl,
                ["__RequestVerificationToken"] = antiforgeryToken,
            }));
        Assert.That(loginResponse.StatusCode, Is.EqualTo(HttpStatusCode.Found), "expected redirect after login");

        // The cookie is now valid and carries this user's id — but delete
        // the underlying account before the browser's next hop, simulating
        // an admin removing the account (or a database restore) between
        // login and the browser completing its round trip back to
        // /connect/authorize.
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(identityUserId);
            Assert.That(user, Is.Not.Null);
            var deleteResult = await userManager.DeleteAsync(user!);
            Assert.That(deleteResult.Succeeded, Is.True);
        }

        // Follow back to /connect/authorize with the now-stale cookie.
        // Previously this threw an unhandled InvalidOperationException (an
        // unstyled 500 the browser had no way to recover from). It should
        // instead redirect back to the login page with an explanatory
        // error, exactly like an unauthenticated request would.
        var authorizeAgain = await client.GetAsync(loginResponse.Headers.Location);

        Assert.That(authorizeAgain.StatusCode, Is.EqualTo(HttpStatusCode.Found),
            "a stale session should redirect back to login, not throw");
        var redirectLocation = authorizeAgain.Headers.Location!.OriginalString;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(redirectLocation, Does.Contain("/Account/Login"));
            Assert.That(redirectLocation, Does.Contain("Error="));
        }

        // The stale cookie must actually have been cleared — a follow-up
        // request should render the login page (not loop back into the
        // same failure), showing the explanatory message.
        var loginPageAfter = await client.GetAsync(redirectLocation);
        Assert.That(loginPageAfter.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var loginPageAfterHtml = await loginPageAfter.Content.ReadAsStringAsync();
        Assert.That(loginPageAfterHtml, Does.Contain("Your session is no longer valid"));
    }

    private static string GeneratePkceVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string ComputeS256Challenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string QueryString(Dictionary<string, string> parameters)
    {
        return "?" + string.Join("&", parameters.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
    }
}
