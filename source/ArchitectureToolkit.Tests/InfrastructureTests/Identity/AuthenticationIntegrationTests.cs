using ArchitectureToolkit.Presentation.API;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace ArchitectureToolkit.Tests.InfrastructureTests.Identity;

/// <summary>
/// Boots the real ArchitectureToolkit.Presentation.API host in-memory (via
/// WebApplicationFactory), verifying the actual DI wiring in
/// DependencyInjection.AddIdentityAuthenticationRegistration and
/// Program.cs's app.UseIdentityAuthentication() — not just the isolated
/// AuthenticationConfiguration class.
///
/// Uses /.well-known/openid-configuration specifically because, like
/// /health, it never touches the database (it's static server metadata
/// built from configured options), so this has no dependency on a
/// reachable PostgreSQL instance. The default appsettings.json ships with
/// no configured Authority, so this always exercises the self-hosted
/// OpenIddict server registration path.
/// </summary>
[TestFixture]
public class AuthenticationIntegrationTests
{
    [Test]
    public async Task DiscoveryDocument_Should_BeReachable_And_AdvertiseConfiguredScope()
    {
        using var factory = new WebApplicationFactory<ApiAssemblyMarker>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/.well-known/openid-configuration");
        var body = await response.Content.ReadAsStringAsync();

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(body, Does.Contain("architecturetoolkit-api"));
        Assert.That(body, Does.Contain("authorization_code"));
    }

    [Test]
    public async Task HealthEndpoint_Should_StillBeReachable_WithIdentityRegistered()
    {
        // Guards against the Identity/OpenIddict registration accidentally
        // breaking unrelated endpoints (e.g. via a misordered middleware
        // pipeline in Program.cs).
        using var factory = new WebApplicationFactory<ApiAssemblyMarker>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.NotFound));
    }
}
