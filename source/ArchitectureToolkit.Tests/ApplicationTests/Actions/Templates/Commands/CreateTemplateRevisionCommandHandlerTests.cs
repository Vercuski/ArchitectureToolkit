using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Templates.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Templates.Commands;

[TestFixture]
public class CreateTemplateRevisionCommandHandlerTests
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

    private void Seed(User[] users, Template[] templates)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        // The handler reads Template via commandDbContext.FindAsync, not
        // queryDbContext — see ICommandDbContext.FindAsync's doc comment
        // for why (xmin concurrency-token continuity across DbContexts).
        A.CallTo(() => _commandDbContext.FindAsync<Template>(A<Guid>._, A<CancellationToken>._))
            .ReturnsLazily((Guid id, CancellationToken _) =>
                Task.FromResult(templates.SingleOrDefault(t => t.Id == id)));
    }

    private CreateTemplateRevisionCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        Seed([], []);

        var result = await CreateHandler().Handle(
            new CreateTemplateRevisionCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotArchitect()
    {
        var contributor = new User("Contributor", "contributor@example.com", SystemRole.Contributor);
        Seed([contributor], []);

        var result = await CreateHandler().Handle(
            new CreateTemplateRevisionCommand(contributor.Id, Guid.NewGuid(), null, null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _commandDbContext.Alter(A<Template>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TemplateDoesNotExist()
    {
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        Seed([architect], []);

        var result = await CreateHandler().Handle(
            new CreateTemplateRevisionCommand(architect.Id, Guid.NewGuid(), null, null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_ExpectedRevisionIsStale()
    {
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        var template = new Template(Guid.NewGuid(), "ADR Template");
        template.CreateRevision(null, null, "# v1", architect.Id);
        Seed([architect], [template]);

        var result = await CreateHandler().Handle(
            new CreateTemplateRevisionCommand(architect.Id, template.Id, Guid.NewGuid(), BumpType.Minor, "# v2"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_DatabaseDetectsARace()
    {
        // Simulates the in-memory check passing (correct ExpectedCurrentRevisionId
        // at the time CreateRevision runs) but a concurrent request having
        // already saved a newer revision by the time SaveChanges is called —
        // caught by CommandDbContext's own RevisionConflictException
        // translation of DbUpdateConcurrencyException.
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        var template = new Template(Guid.NewGuid(), "ADR Template");
        template.CreateRevision(null, null, "# v1", architect.Id);
        Seed([architect], [template]);

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .Throws(new RevisionConflictException(template.CurrentRevisionId, Guid.NewGuid()));

        var result = await CreateHandler().Handle(
            new CreateTemplateRevisionCommand(
                architect.Id, template.Id, template.CurrentRevisionId, BumpType.Minor, "# v2"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
    }

    [Test]
    public async Task Handle_Should_CreateRevision_When_ExpectedRevisionMatches()
    {
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        var template = new Template(Guid.NewGuid(), "ADR Template");
        template.CreateRevision(null, null, "# v1", architect.Id);
        var expectedRevisionId = template.CurrentRevisionId;
        Seed([architect], [template]);

        var result = await CreateHandler().Handle(
            new CreateTemplateRevisionCommand(architect.Id, template.Id, expectedRevisionId, BumpType.Minor, "# v2"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Version, Is.EqualTo("1.1.0"));
            Assert.That(result.Value!.BumpType, Is.EqualTo("Minor"));
        }
        A.CallTo(() => _commandDbContext.Insert(A<TemplateRevision>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
