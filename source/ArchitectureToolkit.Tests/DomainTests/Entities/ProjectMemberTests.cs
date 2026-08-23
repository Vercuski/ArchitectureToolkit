using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class ProjectMemberTests
{
    [Test]
    public void Constructor_Should_SetProjectIdUserIdAndRole()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var member = new ProjectMember(projectId, userId, ProjectRole.Editor);

        Assert.That(member.ProjectId, Is.EqualTo(projectId));
        Assert.That(member.UserId, Is.EqualTo(userId));
        Assert.That(member.Role, Is.EqualTo(ProjectRole.Editor));
    }

    [Test]
    public void Constructor_Should_Throw_When_ProjectIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ProjectMember(Guid.Empty, Guid.NewGuid(), ProjectRole.Viewer));
    }

    [Test]
    public void Constructor_Should_Throw_When_UserIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ProjectMember(Guid.NewGuid(), Guid.Empty, ProjectRole.Viewer));
    }
}
