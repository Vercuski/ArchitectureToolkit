using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Users.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Users.Commands;

[TestFixture]
public class SetUserActiveStatusCommandHandlerTests
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

    /// <summary>Same in-memory backing approach as PromoteUserCommandHandlerTests.</summary>
    private void SeedUsers(params User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.ToList()));
    }

    private SetUserActiveStatusCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        SeedUsers(target);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(Guid.NewGuid(), target.Id, false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotAnArchitect()
    {
        var caller = new User("Contributor", "contributor@example.com", SystemRole.Contributor);
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, target.Id, false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        }
        A.CallTo(() => _commandDbContext.Alter(A<User>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TargetDoesNotExist()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        SeedUsers(caller);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, Guid.NewGuid(), false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
        }
    }

    [Test]
    public async Task Handle_Should_DeactivateTarget_And_Save_When_AnotherActiveArchitectRemains()
    {
        var caller = new User("Architect One", "one@example.com", SystemRole.Architect);
        var target = new User("Contributor", "target@example.com", SystemRole.Contributor);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, target.Id, false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.IsActive, Is.False);
            Assert.That(target.IsActive, Is.False);
        }
        A.CallTo(() => _commandDbContext.Alter(target)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_ReactivateTarget_When_CurrentlyInactive()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        var target = new User("Contributor", "target@example.com", SystemRole.Contributor);
        target.SetActiveStatus(false);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, target.Id, true), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(target.IsActive, Is.True);
        }
        A.CallTo(() => _commandDbContext.Alter(target)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_AllowSelfDeactivation_When_AnotherActiveArchitectRemains()
    {
        var caller = new User("Architect One", "one@example.com", SystemRole.Architect);
        var otherArchitect = new User("Architect Two", "two@example.com", SystemRole.Architect);
        SeedUsers(caller, otherArchitect);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, caller.Id, false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(caller.IsActive, Is.False);
        }
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_DeactivatingTheLastRemainingActiveArchitect()
    {
        var caller = new User("Sole Active Architect", "architect@example.com", SystemRole.Architect);
        SeedUsers(caller);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, caller.Id, false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
            Assert.That(caller.IsActive, Is.True);
        }
        A.CallTo(() => _commandDbContext.Alter(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_DeactivatingLastActiveArchitect_EvenIfInactiveArchitectsExist()
    {
        // An already-inactive Architect elsewhere must not count toward
        // "another active architect remains" (ADR-0017) — same invariant
        // PromoteUserCommandHandlerTests verifies from the demotion side.
        var caller = new User("Sole Active Architect", "active@example.com", SystemRole.Architect);
        var inactiveArchitect = new User("Inactive Architect", "inactive@example.com", SystemRole.Architect);
        inactiveArchitect.SetActiveStatus(false);
        SeedUsers(caller, inactiveArchitect);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, caller.Id, false), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        }
    }

    [Test]
    public async Task Handle_Should_AllowDeactivatingAnAlreadyInactiveArchitect_RegardlessOfActiveArchitectCount()
    {
        // Target is already inactive, so deactivating them again is a
        // no-op that never touches the active-architect count — must
        // succeed even though the caller is the only active architect.
        var caller = new User("Sole Active Architect", "active@example.com", SystemRole.Architect);
        var target = new User("Already Inactive Architect", "inactive@example.com", SystemRole.Architect);
        target.SetActiveStatus(false);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, target.Id, false), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public async Task Handle_Should_Succeed_When_NewStatusEqualsCurrentStatus()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new SetUserActiveStatusCommand(caller.Id, target.Id, true), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.IsActive, Is.True);
        }
    }
}
