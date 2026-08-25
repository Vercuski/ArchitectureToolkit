using System.Security.Claims;
using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;
using ArchitectureToolkit.Persistence.UserProvisioning;

namespace ArchitectureToolkit.Tests.PersistenceTests.UserProvisioning;

[TestFixture]
public class UserProvisioningServiceTests
{
    private IQueryDbContext _queryDbContext = null!;
    private ICommandDbContext _commandDbContext = null!;
    private IUnitOfWork _unitOfWork = null!;
    private IUnitOfWorkTransaction _transaction = null!;
    private ITemplateLibrarySource _templateLibrarySource = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
        _commandDbContext = A.Fake<ICommandDbContext>();
        _unitOfWork = A.Fake<IUnitOfWork>();
        _transaction = A.Fake<IUnitOfWorkTransaction>();
        _templateLibrarySource = A.Fake<ITemplateLibrarySource>();

        A.CallTo(() => _unitOfWork.BeginTransactionAsync(A<CancellationToken>._)).Returns(_transaction);
        A.CallTo(() => _templateLibrarySource.GetCategoriesAsync(A<CancellationToken>._))
            .Returns([]);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _transaction.DisposeAsync();
    }

    private void Seed(User[]? users = null, UserIdentity[]? identities = null, Template[]? templates = null)
    {
        users ??= [];
        identities ??= [];
        templates ??= [];

        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.Set<UserIdentity>()).Returns(identities.AsQueryable());
        A.CallTo(() => _queryDbContext.Set<Template>()).Returns(templates.AsQueryable());

        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<UserIdentity>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<UserIdentity> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.ToList()));
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<Template>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Template> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    private UserProvisioningService CreateService() =>
        new(_commandDbContext, _queryDbContext, _unitOfWork, _templateLibrarySource);

    private static ClaimsPrincipal CreatePrincipal(
        string? subject = "subject-123",
        string? issuer = "https://issuer.example.com",
        string? name = "Scott Vercuski",
        string? email = "scott@example.com",
        string? idp = null)
    {
        var claims = new List<Claim>();
        if (subject is not null)
        {
            claims.Add(new Claim("sub", subject, ClaimValueTypes.String, issuer ?? ClaimsIdentity.DefaultIssuer));
        }
        if (name is not null)
        {
            claims.Add(new Claim("name", name));
        }
        if (email is not null)
        {
            claims.Add(new Claim("email", email));
        }
        if (idp is not null)
        {
            claims.Add(new Claim("idp", idp));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthType"));
    }

    [Test]
    public async Task Handle_Should_ReturnValidationFailure_When_SubjectClaimIsMissing()
    {
        Seed();

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal(subject: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnValidationFailure_When_IssuerIsMissing()
    {
        Seed();

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal(issuer: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnExistingUser_When_IdentityAlreadyLinked()
    {
        var existingUser = new User("Existing User", "existing@example.com", SystemRole.Contributor);
        var existingIdentity = new UserIdentity(
            existingUser.Id, "https://issuer.example.com", "subject-123", "Example IdP");
        Seed(users: [existingUser], identities: [existingIdentity]);

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(existingUser));
        }
        A.CallTo(() => _unitOfWork.BeginTransactionAsync(A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _commandDbContext.Insert(A<User>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_LinkedIdentityReferencesMissingUser()
    {
        var orphanIdentity = new UserIdentity(
            Guid.NewGuid(), "https://issuer.example.com", "subject-123", "Example IdP");
        Seed(identities: [orphanIdentity]);

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnValidationFailure_When_NameClaimIsMissing_ForNewUser()
    {
        Seed();

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal(name: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnValidationFailure_When_EmailClaimIsMissing_ForNewUser()
    {
        Seed();

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal(email: null));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        }
    }

    [Test]
    public async Task Handle_Should_ProvisionAsContributor_When_OtherUsersAlreadyExist()
    {
        var otherUser = new User("Other User", "other@example.com", SystemRole.Architect);
        Seed(users: [otherUser]);

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.SystemRole, Is.EqualTo(SystemRole.Contributor));
        }
        A.CallTo(() => _templateLibrarySource.GetCategoriesAsync(A<CancellationToken>._)).MustNotHaveHappened();
        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_ProvisionAsArchitect_And_CreateIdentity_When_NoUsersExist()
    {
        Seed();

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.SystemRole, Is.EqualTo(SystemRole.Architect));
        }
        A.CallTo(() => _commandDbContext.Insert(A<User>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _commandDbContext.Insert(A<UserIdentity>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Handle_Should_NotSeedTemplates_When_FirstUser_But_TemplatesAlreadyExist()
    {
        var existingTemplate = new Template(Guid.NewGuid(), "Already Seeded");
        Seed(templates: [existingTemplate]);

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.SystemRole, Is.EqualTo(SystemRole.Architect));
        }
        A.CallTo(() => _templateLibrarySource.GetCategoriesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_SeedTemplateLibrary_When_FirstUser_And_TemplatesAreEmpty()
    {
        Seed();
        var libraryCategories = new[]
        {
            new TemplateLibraryCategory("00-vision-and-strategy", "Vision & Strategy",
            [
                new TemplateLibraryFile("Architecture Vision", "---\ntitle: Architecture Vision\n---\n\nBody"),
                new TemplateLibraryFile("Business Case", "---\ntitle: Business Case\n---\n\nBody"),
            ]),
            new TemplateLibraryCategory("11-handover", "Handover",
            [
                new TemplateLibraryFile("As-Built Documentation", "---\ntitle: As-Built Documentation\n---\n\nBody"),
            ]),
        };
        A.CallTo(() => _templateLibrarySource.GetCategoriesAsync(A<CancellationToken>._)).Returns(libraryCategories);

        var insertedCategories = new List<Category>();
        var insertedTemplates = new List<Template>();
        A.CallTo(() => _commandDbContext.Insert(A<Category>._)).Invokes((Category c) => insertedCategories.Add(c));
        A.CallTo(() => _commandDbContext.Insert(A<Template>._)).Invokes((Template t) => insertedTemplates.Add(t));

        var result = await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal());

        Assert.That(result.IsSuccess, Is.True);
        var newUser = result.Value!;

        Assert.That(insertedCategories, Has.Count.EqualTo(2));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(insertedCategories.Select(c => c.Code),
                    Is.EquivalentTo(["00-vision-and-strategy", "11-handover"]));

            Assert.That(insertedTemplates, Has.Count.EqualTo(3));
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(insertedTemplates.Select(t => t.Name),
                    Is.EquivalentTo(["Architecture Vision", "Business Case", "As-Built Documentation"]));

            // Every seeded template's first revision is authored by the new
            // user, seeded at 1.0.0 — per ADR-0014, "no sentinel or system
            // user is introduced."
            Assert.That(insertedTemplates, Has.All.Matches<Template>(t =>
                t.Revisions.Single().AuthorId == newUser.Id
                && t.Revisions.Single().Version.Equals(VersionNumber.Initial)));
        }

        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Handle_Should_RollbackTransaction_And_Rethrow_When_SavingFails()
    {
        Seed();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .Throws(new InvalidOperationException("simulated save failure"));

        Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService().ResolveOrProvisionUserAsync(CreatePrincipal()));

        A.CallTo(() => _transaction.RollbackAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _transaction.CommitAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_UseIdpClaim_ForProviderLabel_When_Present()
    {
        Seed();
        UserIdentity? capturedIdentity = null;
        A.CallTo(() => _commandDbContext.Insert(A<UserIdentity>._)).Invokes((UserIdentity i) => capturedIdentity = i);

        await CreateService().ResolveOrProvisionUserAsync(CreatePrincipal(idp: "Google Workspace"));

        Assert.That(capturedIdentity!.ProviderLabel, Is.EqualTo("Google Workspace"));
    }

    [Test]
    public async Task Handle_Should_DeriveProviderLabel_FromIssuerHost_When_NoIdpClaim()
    {
        Seed();
        UserIdentity? capturedIdentity = null;
        A.CallTo(() => _commandDbContext.Insert(A<UserIdentity>._)).Invokes((UserIdentity i) => capturedIdentity = i);

        await CreateService().ResolveOrProvisionUserAsync(
            CreatePrincipal(issuer: "https://accounts.example-idp.com/tenant/abc"));

        Assert.That(capturedIdentity!.ProviderLabel, Is.EqualTo("accounts.example-idp.com"));
    }
}
