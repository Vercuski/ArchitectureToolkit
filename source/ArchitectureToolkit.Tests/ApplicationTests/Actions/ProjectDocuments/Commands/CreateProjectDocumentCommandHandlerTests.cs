using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.ProjectDocuments.Commands;

[TestFixture]
public class CreateProjectDocumentCommandHandlerTests
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

    private void Seed(
        ProjectMember[] members, Category[] categories, TemplateRevision[] templateRevisions)
    {
        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<Category>()).Returns(categories.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<Category>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Category> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<TemplateRevision>()).Returns(templateRevisions.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<TemplateRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<TemplateRevision> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private CreateProjectDocumentCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        Seed([], [], []);

        var result = await CreateHandler().Handle(
            new CreateProjectDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Vision Doc", null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsViewer()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Viewer);
        Seed([membership], [], []);

        var result = await CreateHandler().Handle(
            new CreateProjectDocumentCommand(callerId, projectId, Guid.NewGuid(), "Vision Doc", null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _commandDbContext.Insert(A<ProjectDocument>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CategoryDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Editor);
        Seed([membership], [], []);

        var result = await CreateHandler().Handle(
            new CreateProjectDocumentCommand(callerId, projectId, Guid.NewGuid(), "Vision Doc", null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_SourceTemplateRevisionDoesNotExist()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Owner);
        var category = new Category("00-vision", "Vision");
        Seed([membership], [category], []);

        var result = await CreateHandler().Handle(
            new CreateProjectDocumentCommand(callerId, projectId, category.Id, "Vision Doc", Guid.NewGuid(), "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnValidation_When_TitleIsEmpty()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Owner);
        var category = new Category("00-vision", "Vision");
        Seed([membership], [category], []);

        var result = await CreateHandler().Handle(
            new CreateProjectDocumentCommand(callerId, projectId, category.Id, "  ", null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Validation));
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_CreateDocumentWithFirstRevision_When_CallerIsEditor()
    {
        var projectId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Editor);
        var category = new Category("00-vision", "Vision");
        var authorId = Guid.NewGuid();
        var sourceTemplate = new Template(category.Id, "Vision Template");
        var sourceRevision = sourceTemplate.CreateRevision(null, null, "# Template content", authorId);
        Seed([membership], [category], [sourceRevision]);

        var result = await CreateHandler().Handle(
            new CreateProjectDocumentCommand(
                callerId, projectId, category.Id, "My Vision Doc", sourceRevision.Id, "# Seeded content"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Title, Is.EqualTo("My Vision Doc"));
            Assert.That(result.Value!.CurrentVersion, Is.EqualTo("1.0.0"));
            Assert.That(result.Value!.Content, Is.EqualTo("# Seeded content"));
        }
        A.CallTo(() => _commandDbContext.Insert(A<ProjectDocument>.That.Matches(d => d.Title == "My Vision Doc")))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
