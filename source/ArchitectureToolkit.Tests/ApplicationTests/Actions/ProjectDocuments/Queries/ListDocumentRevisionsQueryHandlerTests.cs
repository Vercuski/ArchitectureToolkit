using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.ProjectDocuments.Queries;

[TestFixture]
public class ListDocumentRevisionsQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(ProjectDocument[] documents, ProjectMember[] members, DocumentRevision[] revisions)
    {
        A.CallTo(() => _queryDbContext.Set<ProjectDocument>()).Returns(documents.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectDocument>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectDocument> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<DocumentRevision>()).Returns(revisions.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<DocumentRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<DocumentRevision> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_DocumentDoesNotExist()
    {
        Seed([], [], []);

        var result = await new ListDocumentRevisionsQueryHandler(_queryDbContext)
            .Handle(new ListDocumentRevisionsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        var revision = document.CreateRevision(null, null, "# v1", Guid.NewGuid());
        var otherMember = new ProjectMember(document.ProjectId, Guid.NewGuid(), ProjectRole.Owner);
        Seed([document], [otherMember], [revision]);

        var result = await new ListDocumentRevisionsQueryHandler(_queryDbContext)
            .Handle(new ListDocumentRevisionsQuery(Guid.NewGuid(), document.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnAllRevisions_NewestFirst_When_CallerIsAMember()
    {
        var callerId = Guid.NewGuid();
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Vision Doc");
        var v1 = document.CreateRevision(null, null, "# v1", callerId);
        var v2 = document.CreateRevision(v1.Id, BumpType.Minor, "# v2", callerId);
        var membership = new ProjectMember(document.ProjectId, callerId, ProjectRole.Viewer);
        Seed([document], [membership], [v1, v2]);

        var result = await new ListDocumentRevisionsQueryHandler(_queryDbContext)
            .Handle(new ListDocumentRevisionsQuery(callerId, document.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(2));
            Assert.That(result.Value!.First().Id, Is.EqualTo(v2.Id), "newest revision should be first");
        }
    }
}
