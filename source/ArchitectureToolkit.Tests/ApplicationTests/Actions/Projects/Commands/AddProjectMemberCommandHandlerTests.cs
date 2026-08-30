using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Commands;

[TestFixture]
public class AddProjectMemberCommandHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;
    private ICommandDbContext _commandDbContext = null!;
    private IUnitOfWork _unitOfWork = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
        _commandDbContext = A.Fake<ICommandDbContext>();
        _unitOfWork = A.Fake<IUnitOfWork>();
    }

    private void Seed(Project[] projects, ProjectMember[] members, User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<Project>()).Returns(projects.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<Project>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Project> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private AddProjectMemberCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_ProjectDoesNotExist()
    {
        Seed([], [], []);

        var result = await CreateHandler().Handle(
            new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ProjectRole.Viewer),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        var project = new Project("P");
        Seed([project], [], []);

        var result = await CreateHandler().Handle(
            new AddProjectMemberCommand(Guid.NewGuid(), project.Id, Guid.NewGuid(), ProjectRole.Viewer),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotOwner()
    {
        var project = new Project("P");
        var callerId = Guid.NewGuid();
        var callerMembership = new ProjectMember(project.Id, callerId, ProjectRole.Editor);
        Seed([project], [callerMembership], []);

        var result = await CreateHandler().Handle(
            new AddProjectMemberCommand(callerId, project.Id, Guid.NewGuid(), ProjectRole.Viewer),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _commandDbContext.Insert(A<ProjectMember>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TargetUserDoesNotExist()
    {
        var project = new Project("P");
        var callerId = Guid.NewGuid();
        var callerMembership = new ProjectMember(project.Id, callerId, ProjectRole.Owner);
        Seed([project], [callerMembership], []);

        var result = await CreateHandler().Handle(
            new AddProjectMemberCommand(callerId, project.Id, Guid.NewGuid(), ProjectRole.Viewer),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_TargetAlreadyAMember()
    {
        var project = new Project("P");
        var callerId = Guid.NewGuid();
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        var callerMembership = new ProjectMember(project.Id, callerId, ProjectRole.Owner);
        var existingTargetMembership = new ProjectMember(project.Id, target.Id, ProjectRole.Viewer);
        Seed([project], [callerMembership, existingTargetMembership], [target]);

        var result = await CreateHandler().Handle(
            new AddProjectMemberCommand(callerId, project.Id, target.Id, ProjectRole.Editor),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        A.CallTo(() => _commandDbContext.Insert(A<ProjectMember>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_AddMember_When_CallerIsOwnerAndTargetIsNew()
    {
        var project = new Project("P");
        var callerId = Guid.NewGuid();
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        var callerMembership = new ProjectMember(project.Id, callerId, ProjectRole.Owner);
        Seed([project], [callerMembership], [target]);

        var result = await CreateHandler().Handle(
            new AddProjectMemberCommand(callerId, project.Id, target.Id, ProjectRole.Editor),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Role, Is.EqualTo(ProjectRole.Editor));
            Assert.That(result.Value!.UserName, Is.EqualTo("Target"));
        }
        A.CallTo(() => _commandDbContext.Insert(A<ProjectMember>.That.Matches(
            m => m.UserId == target.Id && m.Role == ProjectRole.Editor))).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
