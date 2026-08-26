using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Templates.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Templates.Commands;

[TestFixture]
public class CreateTemplateCommandHandlerTests
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

    private void Seed(User[] users, Category[] categories)
    {
        A.CallTo(() => _queryDbContext.Set<User>()).Returns(users.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<User>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<User> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<Category>()).Returns(categories.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<Category>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Category> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private CreateTemplateCommandHandler CreateHandler() =>
        new(_commandDbContext, _queryDbContext, _unitOfWork);

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerDoesNotExist()
    {
        Seed([], []);

        var result = await CreateHandler().Handle(
            new CreateTemplateCommand(Guid.NewGuid(), Guid.NewGuid(), "ADR Template", "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsNotArchitect()
    {
        var contributor = new User("Contributor", "contributor@example.com", SystemRole.Contributor);
        Seed([contributor], []);

        var result = await CreateHandler().Handle(
            new CreateTemplateCommand(contributor.Id, Guid.NewGuid(), "ADR Template", "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _commandDbContext.Insert(A<Template>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CategoryDoesNotExist()
    {
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        Seed([architect], []);

        var result = await CreateHandler().Handle(
            new CreateTemplateCommand(architect.Id, Guid.NewGuid(), "ADR Template", "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnValidation_When_NameIsEmpty()
    {
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        var category = new Category("00-vision", "Vision");
        Seed([architect], [category]);

        var result = await CreateHandler().Handle(
            new CreateTemplateCommand(architect.Id, category.Id, "  ", "content"), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_CreateTemplateWithFirstRevision_When_CallerIsArchitect()
    {
        var architect = new User("Architect", "architect@example.com", SystemRole.Architect);
        var category = new Category("00-vision", "Vision");
        Seed([architect], [category]);

        var result = await CreateHandler().Handle(
            new CreateTemplateCommand(architect.Id, category.Id, "ADR Template", "# content"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Name, Is.EqualTo("ADR Template"));
            Assert.That(result.Value!.CurrentVersion, Is.EqualTo("1.0.0"));
            Assert.That(result.Value!.Content, Is.EqualTo("# content"));
        }
        A.CallTo(() => _commandDbContext.Insert(A<Template>.That.Matches(t => t.Name == "ADR Template")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
