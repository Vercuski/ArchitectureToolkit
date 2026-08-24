using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class UserTests
{
    [Test]
    public void Constructor_Should_SetNameEmailAndSystemRole()
    {
        var user = new User("Scott Vercuski", "scott@example.com", SystemRole.Contributor);

        Assert.That(user.Name, Is.EqualTo("Scott Vercuski"));
        Assert.That(user.Email, Is.EqualTo("scott@example.com"));
        Assert.That(user.SystemRole, Is.EqualTo(SystemRole.Contributor));
        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_NameIsMissing(string? name)
    {
        Assert.Throws<ArgumentException>(() => new User(name!, "scott@example.com", SystemRole.Contributor));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_EmailIsMissing(string? email)
    {
        Assert.Throws<ArgumentException>(() => new User("Scott Vercuski", email!, SystemRole.Contributor));
    }

    [Test]
    public void PromoteToArchitect_Should_SetSystemRoleToArchitect()
    {
        var user = new User("Scott Vercuski", "scott@example.com", SystemRole.Contributor);

        user.PromoteToArchitect();

        Assert.That(user.SystemRole, Is.EqualTo(SystemRole.Architect));
    }

    [Test]
    public void PromoteToArchitect_Should_BeIdempotent_WhenAlreadyArchitect()
    {
        var user = new User("Scott Vercuski", "scott@example.com", SystemRole.Architect);

        user.PromoteToArchitect();

        Assert.That(user.SystemRole, Is.EqualTo(SystemRole.Architect));
    }

    [TestCase(SystemRole.Contributor, SystemRole.Architect)]
    [TestCase(SystemRole.Architect, SystemRole.Contributor)]
    public void SetSystemRole_Should_SetToWhicheverRoleIsGiven(SystemRole initialRole, SystemRole newRole)
    {
        // Unlike PromoteToArchitect (which only ever grants Architect),
        // SetSystemRole is the general mechanism PromoteUserCommand uses to
        // promote OR demote a target user (ADR-0009).
        var user = new User("Scott Vercuski", "scott@example.com", initialRole);

        user.SetSystemRole(newRole);

        Assert.That(user.SystemRole, Is.EqualTo(newRole));
    }
}
