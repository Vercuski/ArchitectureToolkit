using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Queries;

[TestFixture]
public class ListProjectsQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(Project[] projects, ProjectMember[] members)
    {
        A.CallTo(() => _queryDbContext.Set<Project>()).Returns(projects.AsQueryable());
        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<Project>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Project> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnOnlyProjectsCallerIsMemberOf()
    {
        var callerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var myProject = new Project("Mine");
        var otherProject = new Project("Not Mine");

        var members = new[]
        {
            new ProjectMember(myProject.Id, callerId, ProjectRole.Owner),
            new ProjectMember(otherProject.Id, otherUserId, ProjectRole.Owner)
        };
        Seed([myProject, otherProject], members);

        var result = await new ListProjectsQueryHandler(_queryDbContext)
            .Handle(new ListProjectsQuery(callerId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(1));
            Assert.That(result.Value!.Single().Name, Is.EqualTo("Mine"));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnEmpty_When_CallerHasNoProjects()
    {
        Seed([], []);

        var result = await new ListProjectsQueryHandler(_queryDbContext)
            .Handle(new ListProjectsQuery(Guid.NewGuid()), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Empty);
        }
    }
}
