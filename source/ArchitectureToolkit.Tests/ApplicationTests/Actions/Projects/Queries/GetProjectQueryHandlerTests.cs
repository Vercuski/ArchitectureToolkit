using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Queries;

[TestFixture]
public class GetProjectQueryHandlerTests
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
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<Project>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Project> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private GetProjectQueryHandler CreateHandler()
    {
        return new(_queryDbContext);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_ProjectDoesNotExist()
    {
        Seed([], []);

        var result = await CreateHandler().Handle(
            new GetProjectQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        var project = new Project("Secret Project");
        var otherUserId = Guid.NewGuid();
        var member = new ProjectMember(project.Id, otherUserId, ProjectRole.Owner);
        Seed([project], [member]);

        // NotFound, not Forbidden — a non-member shouldn't be able to
        // confirm the project even exists.
        var result = await CreateHandler().Handle(
            new GetProjectQuery(Guid.NewGuid(), project.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnProject_When_CallerIsAMember()
    {
        var project = new Project("My Project");
        var callerId = Guid.NewGuid();
        var member = new ProjectMember(project.Id, callerId, ProjectRole.Viewer);
        Seed([project], [member]);

        var result = await CreateHandler().Handle(new GetProjectQuery(callerId, project.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Name, Is.EqualTo("My Project"));
        }
    }
}
