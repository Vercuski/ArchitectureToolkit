using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class ProjectTests
{
    [Test]
    public void Constructor_Should_SetName()
    {
        var project = new Project("ArchitectureToolkit Rollout");

        Assert.That(project.Name, Is.EqualTo("ArchitectureToolkit Rollout"));
        Assert.That(project.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_NameIsMissing(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Project(name!));
    }
}
