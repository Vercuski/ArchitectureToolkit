using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class UserIdentityTests
{
    [Test]
    public void Constructor_Should_SetAllFields_And_LinkedAtToUtcNow()
    {
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var identity = new UserIdentity(userId, "https://accounts.google.com", "subject-123", "Google");

        var after = DateTime.UtcNow;

        Assert.That(identity.UserId, Is.EqualTo(userId));
        Assert.That(identity.Issuer, Is.EqualTo("https://accounts.google.com"));
        Assert.That(identity.ExternalSubjectId, Is.EqualTo("subject-123"));
        Assert.That(identity.ProviderLabel, Is.EqualTo("Google"));
        Assert.That(identity.LinkedAt, Is.InRange(before, after));
    }

    [Test]
    public void Constructor_Should_Throw_When_UserIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new UserIdentity(Guid.Empty, "https://accounts.google.com", "subject-123", "Google"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_IssuerIsMissing(string? issuer)
    {
        Assert.Throws<ArgumentException>(() =>
            new UserIdentity(Guid.NewGuid(), issuer!, "subject-123", "Google"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_ExternalSubjectIdIsMissing(string? externalSubjectId)
    {
        Assert.Throws<ArgumentException>(() =>
            new UserIdentity(Guid.NewGuid(), "https://accounts.google.com", externalSubjectId!, "Google"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_ProviderLabelIsMissing(string? providerLabel)
    {
        Assert.Throws<ArgumentException>(() =>
            new UserIdentity(Guid.NewGuid(), "https://accounts.google.com", "subject-123", providerLabel!));
    }
}
