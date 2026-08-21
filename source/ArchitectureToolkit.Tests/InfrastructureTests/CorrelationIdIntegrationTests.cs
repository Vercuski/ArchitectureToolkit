using ArchitectureToolkit.Infrastructure.Correlation;
using ArchitectureToolkit.Presentation.API;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ArchitectureToolkit.Tests.InfrastructureTests;

/// <summary>
/// Boots the real ArchitectureToolkit.Presentation.API host in-memory (via WebApplicationFactory) and
/// exercises it through an actual HttpClient, so this verifies the real Program.cs wiring —
/// including middleware order — rather than just the CorrelationIdMiddleware class in isolation.
/// Uses the /health endpoint specifically because it never touches the database, so this test
/// has no dependency on a real SQL Server/PostgreSQL/MySQL instance being reachable.
/// </summary>
[TestFixture]
public class CorrelationIdIntegrationTests
{
    [SetUp]
    public void SetUp()
    {

    }

    [TearDown]
    public void TearDown()
    {
    }

    [Test]
    public async Task HealthEndpoint_Should_ReturnCorrelationIdResponseHeader()
    {
        using var factory = new WebApplicationFactory<ApiAssemblyMarker>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.That(response.Headers.TryGetValues(CorrelationIdMiddleware.HeaderName, out var values), Is.True);
        var headerValue = values!.Single();
        Assert.That(Guid.TryParse(headerValue, out _), Is.True);
    }

    [Test]
    public async Task Requests_WithoutIncomingHeader_Should_GetADifferentCorrelationIdEachTime()
    {
        using var factory = new WebApplicationFactory<ApiAssemblyMarker>();
        var client = factory.CreateClient();

        var first = await client.GetAsync("/health");
        var second = await client.GetAsync("/health");

        var firstId = first.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        var secondId = second.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();

        Assert.That(firstId, Is.Not.EqualTo(secondId));
    }
}
