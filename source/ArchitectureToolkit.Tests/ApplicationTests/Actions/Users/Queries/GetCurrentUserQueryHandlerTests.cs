using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Users.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Users.Queries;

[TestFixture]
public class GetCurrentUserQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        Seed([]);

        var result = await new GetCurrentUserQueryHandler(_queryDbContext)
            .Handle(new GetCurrentUserQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnCallerProfile_When_CallerExists()
    {
        var user = new User("Ada Lovelace", "ada@example.com", SystemRole.Architect);
        Seed([user]);

        var result = await new GetCurrentUserQueryHandler(_queryDbContext)
            .Handle(new GetCurrentUserQuery(user.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Id, Is.EqualTo(user.Id));
            Assert.That(result.Value!.Name, Is.EqualTo("Ada Lovelace"));
            Assert.That(result.Value!.Email, Is.EqualTo("ada@example.com"));
            Assert.That(result.Value!.SystemRole, Is.EqualTo("Architect"));
        }
    }
}
