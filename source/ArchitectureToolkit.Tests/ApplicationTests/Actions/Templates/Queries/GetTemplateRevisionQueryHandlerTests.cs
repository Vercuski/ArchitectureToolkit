using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Templates.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Templates.Queries;

[TestFixture]
public class GetTemplateRevisionQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(TemplateRevision[] revisions)
    {
        A.CallTo(() => _queryDbContext.Set<TemplateRevision>()).Returns(revisions.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<TemplateRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<TemplateRevision> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_RevisionDoesNotExist()
    {
        Seed([]);

        var result = await new GetTemplateRevisionQueryHandler(_queryDbContext)
            .Handle(new GetTemplateRevisionQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_RevisionBelongsToADifferentTemplate()
    {
        var authorId = Guid.NewGuid();
        var template = new Template(Guid.NewGuid(), "ADR Template");
        var revision = template.CreateRevision(null, null, "# v1", authorId);
        Seed([revision]);

        var result = await new GetTemplateRevisionQueryHandler(_queryDbContext)
            .Handle(new GetTemplateRevisionQuery(Guid.NewGuid(), revision.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnRevision_WithContent_When_ItBelongsToTheTemplate()
    {
        var authorId = Guid.NewGuid();
        var template = new Template(Guid.NewGuid(), "ADR Template");
        var revision = template.CreateRevision(null, null, "# v1 content", authorId);
        Seed([revision]);

        var result = await new GetTemplateRevisionQueryHandler(_queryDbContext)
            .Handle(new GetTemplateRevisionQuery(template.Id, revision.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Content, Is.EqualTo("# v1 content"));
            Assert.That(result.Value!.Version, Is.EqualTo("1.0.0"));
        }
    }
}
