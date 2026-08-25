using ArchitectureToolkit.Infrastructure.Identity;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ArchitectureToolkit.Tests.InfrastructureTests.Identity;

/// <summary>
/// Exercises real filesystem I/O in a temp directory (cleaned up per test),
/// since PersistedCertificateProvisioner's entire purpose is disk
/// persistence — a fake/in-memory version of this test would not actually
/// verify the behavior that matters (surviving a process restart).
/// </summary>
[TestFixture]
public class PersistedCertificateProvisionerTests
{
    private string _tempDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "act-cert-tests-" + Guid.NewGuid());
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Test]
    public void GetOrCreate_Should_CreateAndPersist_When_FileDoesNotExist()
    {
        var filePath = Path.Combine(_tempDirectory, "test-signing.pfx");

        using var certificate = PersistedCertificateProvisioner.GetOrCreate(
            filePath, "correct horse battery staple", "ArchitectureToolkit Test Signing");

        Assert.That(File.Exists(filePath), Is.True);
        Assert.That(certificate.Subject, Is.EqualTo("CN=ArchitectureToolkit Test Signing"));
        Assert.That(certificate.HasPrivateKey, Is.True);
    }

    [Test]
    public void GetOrCreate_Should_ReturnSameKey_OnSecondCall()
    {
        var filePath = Path.Combine(_tempDirectory, "test-signing.pfx");

        using var first = PersistedCertificateProvisioner.GetOrCreate(
            filePath, "correct horse battery staple", "ArchitectureToolkit Test Signing");
        using var second = PersistedCertificateProvisioner.GetOrCreate(
            filePath, "correct horse battery staple", "ArchitectureToolkit Test Signing");

        // Same underlying key pair persisted and reloaded, not regenerated —
        // this is exactly the "must survive a restart" requirement.
        Assert.That(second.Thumbprint, Is.EqualTo(first.Thumbprint));
    }

    [Test]
    public void GetOrCreate_Should_ProduceDistinctCertificates_ForDifferentSubjects()
    {
        using var signing = PersistedCertificateProvisioner.GetOrCreate(
            Path.Combine(_tempDirectory, "signing.pfx"),
            "correct horse battery staple",
            "ArchitectureToolkit Signing");
        using var encryption = PersistedCertificateProvisioner.GetOrCreate(
            Path.Combine(_tempDirectory, "encryption.pfx"),
            "correct horse battery staple",
            "ArchitectureToolkit Encryption");

        Assert.That(encryption.Thumbprint, Is.Not.EqualTo(signing.Thumbprint));
    }

    [Test]
    public void GetOrCreate_Should_CreateMissingParentDirectory()
    {
        var nestedPath = Path.Combine(_tempDirectory, "nested", "deeper", "signing.pfx");

        using var certificate = PersistedCertificateProvisioner.GetOrCreate(
            nestedPath, "correct horse battery staple", "ArchitectureToolkit Test Signing");

        Assert.That(File.Exists(nestedPath), Is.True);
    }

    [Test]
    public void GetOrCreate_Should_Throw_When_ReloadedWithWrongPassword()
    {
        var filePath = Path.Combine(_tempDirectory, "test-signing.pfx");
        using var created = PersistedCertificateProvisioner.GetOrCreate(
            filePath, "correct horse battery staple", "ArchitectureToolkit Test Signing");

        Assert.Throws<CryptographicException>(() =>
            PersistedCertificateProvisioner.GetOrCreate(filePath, "wrong password", "unused"));
    }
}
