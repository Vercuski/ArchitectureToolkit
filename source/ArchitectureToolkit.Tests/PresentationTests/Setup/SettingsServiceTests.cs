using ArchitectureToolkit.Infrastructure.Setup;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using ArchitectureToolkit.Presentation.API.Setup;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchitectureToolkit.Tests.PresentationTests.Setup;

[TestFixture]
public class SettingsServiceTests
{
    private IAppConfigurationStore _configurationStore = null!;
    private IHostApplicationLifetime _applicationLifetime = null!;

    [SetUp]
    public void SetUp()
    {
        _configurationStore = A.Fake<IAppConfigurationStore>();
        _applicationLifetime = A.Fake<IHostApplicationLifetime>();
    }

    private static PersistedAppConfiguration Configuration(
        string? smtpPassword = "current-password", string templateLibraryRootPath = "/app/DocumentationTemplates")
    {
        return new PersistedAppConfiguration
        {
            QueryDbConnection = "Host=db;Database=architecturetoolkit;Username=user;Password=secret",
            CommandDbConnection = "Host=db;Database=architecturetoolkit;Username=user;Password=secret",
            TemplateLibraryRootPath = templateLibraryRootPath,
            Authority = null,
            ClientId = "architecturetoolkit-spa",
            Audience = "architecturetoolkit-api",
            SmtpHost = "smtp.example.com",
            SmtpPort = 587,
            SmtpUsername = "notifications@example.com",
            SmtpPassword = smtpPassword,
            SmtpFromAddress = "no-reply@example.com",
            SmtpFromName = "ArchitectureToolkit",
            SmtpUseSslOnConnect = false,
            AuthKeysPassword = "auth-keys-password",
            PendingInitialUser = null,
        };
    }

    private SettingsService CreateService()
    {
        return new(_configurationStore, _applicationLifetime, NullLogger<SettingsService>.Instance);
    }

    private static UpdateSettingsRequest ValidRequest(
        string templateLibraryRootPath, string? smtpPassword = null) => new()
    {
        TemplateLibraryRootPath = templateLibraryRootPath,
        SmtpHost = "smtp.example.com",
        SmtpPort = 587,
        SmtpUsername = "notifications@example.com",
        SmtpPassword = smtpPassword,
        SmtpFromAddress = "no-reply@example.com",
        SmtpFromName = "ArchitectureToolkit",
        SmtpUseSslOnConnect = false,
    };

    [Test]
    public void GetCurrent_Should_ReturnConfiguredValues_WithoutExposingThePasswordItself()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration(smtpPassword: "super-secret"));

        var result = CreateService().GetCurrent();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.TemplateLibraryRootPath, Is.EqualTo("/app/DocumentationTemplates"));
            Assert.That(result.SmtpHost, Is.EqualTo("smtp.example.com"));
            Assert.That(result.SmtpPort, Is.EqualTo(587));
            Assert.That(result.SmtpUsername, Is.EqualTo("notifications@example.com"));
            Assert.That(result.SmtpPasswordConfigured, Is.True);
            Assert.That(result.SmtpFromAddress, Is.EqualTo("no-reply@example.com"));
        }
    }

    [Test]
    public void GetCurrent_Should_ReportSmtpPasswordConfigured_False_When_NoPasswordIsSet()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration(smtpPassword: null));

        var result = CreateService().GetCurrent();

        Assert.That(result.SmtpPasswordConfigured, Is.False);
    }

    [Test]
    public void GetCurrent_Should_Throw_When_SetupHasNotCompletedYet()
    {
        A.CallTo(() => _configurationStore.Load()).Returns((PersistedAppConfiguration?)null);

        Assert.Throws<InvalidOperationException>(() => CreateService().GetCurrent());
    }

    [Test]
    public void Update_Should_ReturnValidationFailure_When_TemplateLibraryRootPathIsMissing()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration());

        var result = CreateService().Update(ValidRequest(templateLibraryRootPath: "   "));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Any(e => e.Field == nameof(UpdateSettingsRequest.TemplateLibraryRootPath)), Is.True);
        }
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._)).MustNotHaveHappened();
    }

    [Test]
    public void Update_Should_ReturnValidationFailure_When_TemplateLibraryRootPathDoesNotExist()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration());
        var missingPath = Path.Combine(Path.GetTempPath(), "att-settings-tests-" + Guid.NewGuid());

        var result = CreateService().Update(ValidRequest(missingPath));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Any(e => e.Field == nameof(UpdateSettingsRequest.TemplateLibraryRootPath)), Is.True);
        }
    }

    [Test]
    public void Update_Should_ReturnValidationFailure_When_SmtpFromAddressIsMissing()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration());
        var request = ValidRequest(Path.GetTempPath()) with { SmtpFromAddress = "" };

        var result = CreateService().Update(request);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors.Any(e => e.Field == nameof(UpdateSettingsRequest.SmtpFromAddress)), Is.True);
        }
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void Update_Should_ReturnValidationFailure_When_SmtpPortIsOutOfRange(int invalidPort)
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration());
        var request = ValidRequest(Path.GetTempPath()) with { SmtpPort = invalidPort };

        var result = CreateService().Update(request);

        Assert.That(result.Errors.Any(e => e.Field == nameof(UpdateSettingsRequest.SmtpPort)), Is.True);
    }

    [Test]
    public void Update_Should_PreserveTheCurrentSmtpPassword_When_RequestPasswordIsNull()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration(smtpPassword: "still-the-current-one"));
        PersistedAppConfiguration? saved = null;
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._))
            .Invokes((PersistedAppConfiguration c) => saved = c);

        var result = CreateService().Update(ValidRequest(Path.GetTempPath(), smtpPassword: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(saved!.SmtpPassword, Is.EqualTo("still-the-current-one"));
        }
    }

    [Test]
    public void Update_Should_ReplaceTheSmtpPassword_When_RequestSuppliesANewOne()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration(smtpPassword: "old-password"));
        PersistedAppConfiguration? saved = null;
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._))
            .Invokes((PersistedAppConfiguration c) => saved = c);

        CreateService().Update(ValidRequest(Path.GetTempPath(), smtpPassword: "brand-new-password"));

        Assert.That(saved!.SmtpPassword, Is.EqualTo("brand-new-password"));
    }

    [Test]
    public void Update_Should_PreserveFieldsThisServiceDoesNotEdit()
    {
        var current = Configuration();
        A.CallTo(() => _configurationStore.Load()).Returns(current);
        PersistedAppConfiguration? saved = null;
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._))
            .Invokes((PersistedAppConfiguration c) => saved = c);

        CreateService().Update(ValidRequest(Path.GetTempPath()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(saved!.QueryDbConnection, Is.EqualTo(current.QueryDbConnection));
            Assert.That(saved.CommandDbConnection, Is.EqualTo(current.CommandDbConnection));
            Assert.That(saved.Authority, Is.EqualTo(current.Authority));
            Assert.That(saved.ClientId, Is.EqualTo(current.ClientId));
            Assert.That(saved.Audience, Is.EqualTo(current.Audience));
            Assert.That(saved.AuthKeysPassword, Is.EqualTo(current.AuthKeysPassword));
        }
    }

    [Test]
    public void Update_Should_SaveTheEditedFields_OnSuccess()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration());
        PersistedAppConfiguration? saved = null;
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._))
            .Invokes((PersistedAppConfiguration c) => saved = c);
        var tempPath = Path.GetTempPath();

        var result = CreateService().Update(ValidRequest(tempPath) with
        {
            SmtpHost = "smtp.newhost.example.com",
            SmtpFromName = "New From Name",
        });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(saved!.TemplateLibraryRootPath, Is.EqualTo(tempPath));
            Assert.That(saved.SmtpHost, Is.EqualTo("smtp.newhost.example.com"));
            Assert.That(saved.SmtpFromName, Is.EqualTo("New From Name"));
        }
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Update_Should_TreatBlankSmtpHost_AsDisablingEmail()
    {
        A.CallTo(() => _configurationStore.Load()).Returns(Configuration());
        PersistedAppConfiguration? saved = null;
        A.CallTo(() => _configurationStore.Save(A<PersistedAppConfiguration>._))
            .Invokes((PersistedAppConfiguration c) => saved = c);

        CreateService().Update(ValidRequest(Path.GetTempPath()) with { SmtpHost = "   " });

        Assert.That(saved!.SmtpHost, Is.Null);
    }
}
