using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Commands;

[TestFixture]
public class RemoveProjectMemberCommandHandlerTests
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

    private void Seed(ProjectMember[] members)
    {
        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    private RemoveProjectMemberCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        Seed([]);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotOwner()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var caller = new ProjectMember(projectId, callerId, ProjectRole.Editor);
        var target = new ProjectMember(projectId, targetId, ProjectRole.Viewer);
        Seed([caller, target]);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(callerId, projectId, targetId), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _commandDbContext.Delete(A<ProjectMember>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TargetIsNotAMember()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var caller = new ProjectMember(projectId, callerId, ProjectRole.Owner);
        Seed([caller]);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(callerId, projectId, Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_RemovingTheLastRemainingOwner()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var soleOwner = new ProjectMember(projectId, callerId, ProjectRole.Owner);
        Seed([soleOwner]);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(callerId, projectId, callerId), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        A.CallTo(() => _commandDbContext.Delete(A<ProjectMember>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_RemoveNonOwnerMember_When_CallerIsOwner()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var caller = new ProjectMember(projectId, callerId, ProjectRole.Owner);
        var target = new ProjectMember(projectId, targetId, ProjectRole.Viewer);
        Seed([caller, target]);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(callerId, projectId, targetId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.True);
        }
        A.CallTo(() => _commandDbContext.Delete(target)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_RemoveOwner_When_AnotherOwnerRemains()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var caller = new ProjectMember(projectId, callerId, ProjectRole.Owner);
        var target = new ProjectMember(projectId, targetId, ProjectRole.Owner);
        Seed([caller, target]);

        var result = await CreateHandler().Handle(
            new RemoveProjectMemberCommand(callerId, projectId, targetId), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        A.CallTo(() => _commandDbContext.Delete(target)).MustHaveHappenedOnceExactly();
    }
}
