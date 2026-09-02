using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Users.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Users.Commands;

[TestFixture]
public class CreateUserCommandHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;
    private ICommandDbContext _commandDbContext = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IIdentityAccountService _identityAccountService = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
        _commandDbContext = A.Fake<ICommandDbContext>();
        _unitOfWork = A.Fake<IUnitOfWork>();
        _identityAccountService = A.Fake<IIdentityAccountService>();

        A.CallTo(() => _identityAccountService.SupportsPasswordAccounts).Returns(true);
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._))
            .Returns(Result<UserInviteOutcome>.Success(new UserInviteOutcome(EmailSent: true, InviteLink: null)));
    }

    private void SeedUsers(params User[] users)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private CreateUserCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork, _identityAccountService);
    }

    private static User Architect(string email = "architect@example.com")
    {
        return new User("Architect", email, SystemRole.Architect);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        SeedUsers();

        var result = await CreateHandler().Handle(
            new CreateUserCommand(Guid.NewGuid(), "new@example.com", SystemRole.Contributor), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotAnArchitect()
    {
        var caller = new User("Contributor", "contributor@example.com", SystemRole.Contributor);
        SeedUsers(caller);

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "new@example.com", SystemRole.Contributor), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_DeploymentDoesNotSupportPasswordAccounts()
    {
        var caller = Architect();
        SeedUsers(caller);
        A.CallTo(() => _identityAccountService.SupportsPasswordAccounts).Returns(false);

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "new@example.com", SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        }
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _commandDbContext.Insert(A<User>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_EmailAlreadyExists()
    {
        var caller = Architect();
        var existing = new User("Existing", "duplicate@example.com", SystemRole.Contributor);
        SeedUsers(caller, existing);

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "duplicate@example.com", SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        }
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_PropagateInviteFailure_And_NotCreateUser()
    {
        var caller = Architect();
        SeedUsers(caller);
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._))
            .Returns(Result<UserInviteOutcome>.Failure("Could not create identity account.", ResultErrorType.Conflict));

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "new@example.com", SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
            Assert.That(result.Error, Is.EqualTo("Could not create identity account."));
        }
        // The whole point of inviting before creating the domain row
        // (ADR-0018) — a failed invite must never leave a USER row behind.
        A.CallTo(() => _commandDbContext.Insert(A<User>._)).MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_CreateUser_With_ChosenRoleAndActiveTrue_When_InviteSucceeds()
    {
        var caller = Architect();
        SeedUsers(caller);
        User? insertedUser = null;
        A.CallTo(() => _commandDbContext.Insert(A<User>._)).Invokes((User u) => insertedUser = u);

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "new.hire@example.com", SystemRole.Architect), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(insertedUser!.Email, Is.EqualTo("new.hire@example.com"));
            Assert.That(insertedUser.SystemRole, Is.EqualTo(SystemRole.Architect));
            Assert.That(insertedUser.IsActive, Is.True);
        }
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_DerivePlaceholderName_FromEmailLocalPart()
    {
        var caller = Architect();
        SeedUsers(caller);
        User? insertedUser = null;
        A.CallTo(() => _commandDbContext.Insert(A<User>._)).Invokes((User u) => insertedUser = u);

        await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "jane.doe@example.com", SystemRole.Contributor), CancellationToken.None);

        Assert.That(insertedUser!.Name, Is.EqualTo("jane.doe"));
    }

    [Test]
    public async Task Handle_Should_ReturnEmailSentAndNoLink_When_InviteEmailWasSent()
    {
        var caller = Architect();
        SeedUsers(caller);
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._))
            .Returns(Result<UserInviteOutcome>.Success(new UserInviteOutcome(EmailSent: true, InviteLink: null)));

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "new@example.com", SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value!.EmailSent, Is.True);
            Assert.That(result.Value.InviteLink, Is.Null);
        }
    }

    [Test]
    public async Task Handle_Should_ReturnInviteLink_When_EmailWasNotSent()
    {
        var caller = Architect();
        SeedUsers(caller);
        A.CallTo(() => _identityAccountService.InviteAsync(A<string>._, A<CancellationToken>._))
            .Returns(Result<UserInviteOutcome>.Success(
                new UserInviteOutcome(EmailSent: false, InviteLink: "https://app.example.com/set-password?...")));

        var result = await CreateHandler().Handle(
            new CreateUserCommand(caller.Id, "new@example.com", SystemRole.Contributor), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Value!.EmailSent, Is.False);
            Assert.That(result.Value.InviteLink, Is.EqualTo("https://app.example.com/set-password?..."));
        }
    }
}
