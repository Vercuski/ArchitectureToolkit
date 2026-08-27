using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.ProjectDocuments.Queries;

[TestFixture]
public class GetDocumentRevisionQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(DocumentRevision[] revisions, ProjectDocument[] documents, ProjectMember[] members)
    {
        A.CallTo(() => _queryDbContext.Set<DocumentRevision>()).Returns(revisions.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<DocumentRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<DocumentRevision> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectDocument>()).Returns(documents.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectDocument>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectDocument> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_RevisionDoesNotExist()
    {
        Seed([], [], []);

        var result = await new GetDocumentRevisionQueryHandler(_queryDbContext).Handle(
            new GetDocumentRevisionQuery(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_RevisionBelongsToADifferentDocument()
    {
        var authorId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        var revision = document.CreateRevision(null, null, "# v1", authorId);
        var membership = new ProjectMember(document.ProjectId, authorId, ProjectRole.Owner);
        Seed([revision], [document], [membership]);

        var result = await new GetDocumentRevisionQueryHandler(_queryDbContext).Handle(
            new GetDocumentRevisionQuery(authorId, Guid.NewGuid(), revision.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        var authorId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        var revision = document.CreateRevision(null, null, "# v1", authorId);
        var otherMember = new ProjectMember(document.ProjectId, Guid.NewGuid(), ProjectRole.Owner);
        Seed([revision], [document], [otherMember]);

        var result = await new GetDocumentRevisionQueryHandler(_queryDbContext).Handle(
            new GetDocumentRevisionQuery(Guid.NewGuid(), document.Id, revision.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnRevision_WithContent_When_CallerIsAMember()
    {
        var callerId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        var revision = document.CreateRevision(null, null, "# v1 content", callerId);
        var membership = new ProjectMember(document.ProjectId, callerId, ProjectRole.Viewer);
        Seed([revision], [document], [membership]);

        var result = await new GetDocumentRevisionQueryHandler(_queryDbContext).Handle(
            new GetDocumentRevisionQuery(callerId, document.Id, revision.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Content, Is.EqualTo("# v1 content"));
        }
    }
}
