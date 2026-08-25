using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Users.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Users.Commands;

[TestFixture]
public class PromoteUserCommandHandlerTests
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

    /// <summary>
    /// Backs Set&lt;User&gt;() with an in-memory list, and makes
    /// SingleOrDefaultAsync/ToListAsync actually evaluate whatever
    /// IQueryable{User} they're given (via plain LINQ-to-Objects, since the
    /// backing queryable is an in-memory array, not a real EF Core
    /// provider) — rather than stubbing each call's return value
    /// individually, which would break the moment the handler's query
    /// shape changes.
    /// </summary>
    private void SeedUsers(params User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.ToList()));
    }

    private PromoteUserCommandHandler CreateHandler() =>
        new(_commandDbContext, _queryDbContext, _unitOfWork);

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        SeedUsers(target);

        var result = await CreateHandler().Handle(
            new PromoteUserCommand(Guid.NewGuid(), target.Id, SystemRole.Architect), CancellationToken.None);

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
            new PromoteUserCommand(caller.Id, target.Id, SystemRole.Architect), CancellationToken.None);

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
            new PromoteUserCommand(caller.Id, Guid.NewGuid(), SystemRole.Architect), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
        }
    }

    [Test]
    public async Task Handle_Should_PromoteTarget_And_Save_When_CallerIsArchitect()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new PromoteUserCommand(caller.Id, target.Id, SystemRole.Architect), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(SystemRole.Architect));
            Assert.That(target.SystemRole, Is.EqualTo(SystemRole.Architect));
        }
        A.CallTo(() => _commandDbContext.Alter(target)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_DemoteTarget_When_AnotherArchitectRemains()
    {
        var caller = new User("Architect One", "one@example.com", SystemRole.Architect);
        var target = new User("Architect Two", "two@example.com", SystemRole.Architect);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new PromoteUserCommand(caller.Id, target.Id, SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(target.SystemRole, Is.EqualTo(SystemRole.Contributor));
        }
        A.CallTo(() => _commandDbContext.Alter(target)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_DemotingTheLastRemainingArchitect()
    {
        var caller = new User("Sole Architect", "architect@example.com", SystemRole.Architect);
        SeedUsers(caller);

        var result = await CreateHandler().Handle(
            new PromoteUserCommand(caller.Id, caller.Id, SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
            Assert.That(caller.SystemRole, Is.EqualTo(SystemRole.Architect));
        }
        A.CallTo(() => _commandDbContext.Alter(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_Succeed_When_NewRoleEqualsCurrentRole()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        var target = new User("Target", "target@example.com", SystemRole.Contributor);
        SeedUsers(caller, target);

        var result = await CreateHandler().Handle(
            new PromoteUserCommand(caller.Id, target.Id, SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(SystemRole.Contributor));
        }
    }
}
