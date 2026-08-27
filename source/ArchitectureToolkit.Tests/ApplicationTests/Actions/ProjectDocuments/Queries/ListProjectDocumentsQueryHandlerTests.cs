using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.ProjectDocuments.Queries;

[TestFixture]
public class ListProjectDocumentsQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(ProjectMember[] members, ProjectDocument[] documents)
    {
        A.CallTo(() => _queryDbContext.Set<ProjectMember>()).Returns(members.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<ProjectMember>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectMember> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<ProjectDocument>()).Returns(documents.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<ProjectDocument>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<ProjectDocument> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_CallerIsNotAMember()
    {
        Seed([], []);

        var result = await new ListProjectDocumentsQueryHandler(_queryDbContext)
            .Handle(new ListProjectDocumentsQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnOnlyDocumentsForTheRequestedProject()
    {
        var callerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var otherProjectId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Viewer);

        var myDoc = new ProjectDocument(projectId, Guid.NewGuid(), "My Doc");
        myDoc.CreateRevision(null, null, "# content", callerId);

        var otherDoc = new ProjectDocument(otherProjectId, Guid.NewGuid(), "Not Mine");
        otherDoc.CreateRevision(null, null, "# content", callerId);

        Seed([membership], [myDoc, otherDoc]);

        var result = await new ListProjectDocumentsQueryHandler(_queryDbContext)
            .Handle(new ListProjectDocumentsQuery(callerId, projectId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(1));
            Assert.That(result.Value!.Single().Title, Is.EqualTo("My Doc"));
        }
    }

    [Test]
    public async Task Handle_Should_ExcludeDocuments_WithNoRevisionsYet()
    {
        var callerId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var membership = new ProjectMember(projectId, callerId, ProjectRole.Viewer);
        var bareDoc = new ProjectDocument(projectId, Guid.NewGuid(), "No Revisions Yet");
        Seed([membership], [bareDoc]);

        var result = await new ListProjectDocumentsQueryHandler(_queryDbContext)
            .Handle(new ListProjectDocumentsQuery(callerId, projectId), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Empty);
        }
    }
}
