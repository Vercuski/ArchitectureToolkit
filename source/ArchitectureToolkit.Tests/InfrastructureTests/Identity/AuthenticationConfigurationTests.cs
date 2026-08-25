using ArchitectureToolkit.Infrastructure.Identity;

namespace ArchitectureToolkit.Tests.InfrastructureTests.Identity;

[TestFixture]
public class AuthenticationConfigurationTests
{
    [Test]
    public void Defaults_Should_UseSelfHostedProvider()
    {
        var config = new AuthenticationConfiguration();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(config.Authority, Is.Null);
            Assert.That(config.UseSelfHostedProvider, Is.True);
            Assert.That(config.ClientId, Is.EqualTo("architecturetoolkit-spa"));
            Assert.That(config.Audience, Is.EqualTo("architecturetoolkit-api"));
        }
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void UseSelfHostedProvider_Should_BeTrue_When_AuthorityIsNullOrWhitespace(string? authority)
    {
        var config = new AuthenticationConfiguration { Authority = authority };

        Assert.That(config.UseSelfHostedProvider, Is.True);
    }

    [Test]
    public void UseSelfHostedProvider_Should_BeFalse_When_AuthorityIsConfigured()
    {
        var config = new AuthenticationConfiguration { Authority = "https://auth.example.com" };

        Assert.That(config.UseSelfHostedProvider, Is.False);
    }
}
