using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Queries;

[TestFixture]
public class ListProjectMembersQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(ProjectMember[] members, User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.ToList()));

        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        Seed([], []);

        var result = await new ListProjectMembersQueryHandler(_queryDbContext).Handle(
            new ListProjectMembersQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnAllMembers_WithUserDetails_When_CallerIsAMember()
    {
        var projectId = Guid.NewGuid();
        var owner = new User("Owner", "owner@example.com", SystemRole.Contributor);
        var viewer = new User("Viewer", "viewer@example.com", SystemRole.Contributor);

        var ownerMembership = new ProjectMember(projectId, owner.Id, ProjectRole.Owner);
        var viewerMembership = new ProjectMember(projectId, viewer.Id, ProjectRole.Viewer);

        Seed([ownerMembership, viewerMembership], [owner, viewer]);

        var result = await new ListProjectMembersQueryHandler(_queryDbContext).Handle(
            new ListProjectMembersQuery(owner.Id, projectId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(2));
            Assert.That(result.Value!.Any(m => m.UserId == owner.Id && m.Role == ProjectRole.Owner
                && m.UserName == "Owner" && m.UserEmail == "owner@example.com"), Is.True);
            Assert.That(result.Value!.Any(m => m.UserId == viewer.Id && m.Role == ProjectRole.Viewer), Is.True);
        }
    }

    [Test]
    public async Task Handle_Should_OnlyReturnMembersOfTheRequestedProject()
    {
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        var caller = new User("Caller", "caller@example.com", SystemRole.Contributor);
        var otherUser = new User("Other", "other@example.com", SystemRole.Contributor);

        var callerMembership = new ProjectMember(projectId, caller.Id, ProjectRole.Owner);
        var unrelatedMembership = new ProjectMember(otherProjectId, otherUser.Id, ProjectRole.Owner);

        Seed([callerMembership, unrelatedMembership], [caller, otherUser]);

        var result = await new ListProjectMembersQueryHandler(_queryDbContext).Handle(
            new ListProjectMembersQuery(caller.Id, projectId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(1));
            Assert.That(result.Value!.Single().UserId, Is.EqualTo(caller.Id));
        }
    }
}
