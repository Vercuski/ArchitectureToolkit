using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Commands;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.ProjectDocuments.Commands;

[TestFixture]
public class CreateDocumentRevisionCommandHandlerTests
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

    private void Seed(ProjectDocument[] documents, ProjectMember[] members)
    {
        A.CallTo(() => _commandDbContext.FindAsync<ProjectDocument>(A<Guid>._, A<CancellationToken>._))
            .ReturnsLazily((Guid id, CancellationToken _) =>
                Task.FromResult(documents.SingleOrDefault(d => d.Id == id)));

        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    private CreateDocumentRevisionCommandHandler CreateHandler()
    {
        return new(_commandDbContext, _queryDbContext, _unitOfWork);
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_DocumentDoesNotExist()
    {
        Seed([], []);

        var result = await CreateHandler().Handle(
            new CreateDocumentRevisionCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        document.CreateRevision(null, null, "# v1", Guid.NewGuid());
        Seed([document], []);

        var result = await CreateHandler().Handle(
            new CreateDocumentRevisionCommand(Guid.NewGuid(), document.Id, null, null, "content"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnForbidden_When_CallerIsViewer()
    {
        var callerId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        document.CreateRevision(null, null, "# v1", Guid.NewGuid());
        var membership = new ProjectMember(document.ProjectId, callerId, ProjectRole.Viewer);
        Seed([document], [membership]);

        var result = await CreateHandler().Handle(
            new CreateDocumentRevisionCommand(callerId, document.Id, document.CurrentRevisionId, BumpType.Minor, "# v2"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Forbidden));
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_ExpectedRevisionIsStale()
    {
        var callerId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        document.CreateRevision(null, null, "# v1", callerId);
        var membership = new ProjectMember(document.ProjectId, callerId, ProjectRole.Editor);
        Seed([document], [membership]);

        var result = await CreateHandler().Handle(
            new CreateDocumentRevisionCommand(callerId, document.Id, Guid.NewGuid(), BumpType.Minor, "# v2"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task Handle_Should_ReturnConflict_When_DatabaseDetectsARace()
    {
        var callerId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        document.CreateRevision(null, null, "# v1", callerId);
        var membership = new ProjectMember(document.ProjectId, callerId, ProjectRole.Owner);
        Seed([document], [membership]);

        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._))
            .Throws(new RevisionConflictException(document.CurrentRevisionId, Guid.NewGuid()));

        var result = await CreateHandler().Handle(
            new CreateDocumentRevisionCommand(callerId, document.Id, document.CurrentRevisionId, BumpType.Minor, "# v2"),
            CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.Conflict));
    }

    [Test]
    public async Task Handle_Should_CreateRevision_When_ExpectedRevisionMatches()
    {
        var callerId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        document.CreateRevision(null, null, "# v1", callerId);
        var expectedRevisionId = document.CurrentRevisionId;
        var membership = new ProjectMember(document.ProjectId, callerId, ProjectRole.Editor);
        Seed([document], [membership]);

        var result = await CreateHandler().Handle(
            new CreateDocumentRevisionCommand(callerId, document.Id, expectedRevisionId, BumpType.Minor, "# v2"),
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Version, Is.EqualTo("1.1.0"));
            Assert.That(result.Value!.BumpType, Is.EqualTo("Minor"));
        }
        A.CallTo(() => _commandDbContext.Insert(A<DocumentRevision>._)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _commandDbContext.Alter(A<ProjectDocument>._)).MustNotHaveHappened();
        A.CallTo(() => _unitOfWork.SaveChangesAsync(A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }
}
