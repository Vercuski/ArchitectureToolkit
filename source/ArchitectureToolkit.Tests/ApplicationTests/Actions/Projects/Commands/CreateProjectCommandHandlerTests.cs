using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Projects.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Projects.Commands;

[TestFixture]
public class CreateProjectCommandHandlerTests
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

    private void SeedUsers(params User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.SingleOrDefault()));
    }

    private CreateProjectCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        SeedUsers();

        var result = await CreateHandler().Handle(
            new CreateProjectCommand(Guid.NewGuid(), "My Project"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnValidation_When_NameIsEmpty()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        SeedUsers(caller);

        var result = await CreateHandler().Handle(
            new CreateProjectCommand(caller.Id, "   "), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        }
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_CreateProjectAndOwnerMembership_When_CallerExists()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        SeedUsers(caller);

        var result = await CreateHandler().Handle(
            new CreateProjectCommand(caller.Id, "My Project"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Name, Is.EqualTo("My Project"));
        }

        A.CallTo(() => _commandDbContext.Insert(A<Project>.That.Matches(p => p.Name == "My Project")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _commandDbContext.Insert(A<ProjectMember>.That.Matches(
            m => m.UserId == caller.Id && m.Role == ProjectRole.Owner)))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
