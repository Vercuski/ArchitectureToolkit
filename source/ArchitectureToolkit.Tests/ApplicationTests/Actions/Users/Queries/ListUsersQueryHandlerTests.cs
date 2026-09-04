using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Users.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Users.Queries;

[TestFixture]
public class ListUsersQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void SeedUsers(params User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> query, CancellationToken _) => Task.FromResult(query.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        SeedUsers();

        var result = await new ListUsersQueryHandler(_queryDbContext)
            .Handle(new ListUsersQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotAnArchitect()
    {
        var caller = new User("Contributor", "contributor@example.com", SystemRole.Contributor);
        SeedUsers(caller);

        var result = await new ListUsersQueryHandler(_queryDbContext)
            .Handle(new ListUsersQuery(caller.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
    }

    [Test]
    public async Task Handle_Should_ReturnEveryUser_SortedAlphabeticallyByEmail()
    {
        var caller = new User("Architect", "zed@example.com", SystemRole.Architect);
        var userB = new User("Bea", "bea@example.com", SystemRole.Contributor);
        var userM = new User("Mia", "mia@example.com", SystemRole.Contributor);
        SeedUsers(caller, userM, userB);

        var result = await new ListUsersQueryHandler(_queryDbContext)
            .Handle(new ListUsersQuery(caller.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Select(u => u.Email), Is.EqualTo(
                ["bea@example.com", "mia@example.com", "zed@example.com"]));
        }
    }

    [Test]
    public async Task Handle_Should_IncludeIsActive_ForEachUser()
    {
        var caller = new User("Architect", "architect@example.com", SystemRole.Architect);
        var inactiveUser = new User("Inactive", "inactive@example.com", SystemRole.Contributor);
        inactiveUser.SetActiveStatus(false);
        SeedUsers(caller, inactiveUser);

        var result = await new ListUsersQueryHandler(_queryDbContext)
            .Handle(new ListUsersQuery(caller.Id), CancellationToken.None);

        var inactiveDto = result.Value!.Single(u => u.Id == inactiveUser.Id);
        var callerDto = result.Value!.Single(u => u.Id == caller.Id);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(inactiveDto.IsActive, Is.False);
            Assert.That(callerDto.IsActive, Is.True);
        }
    }
}
