using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Templates.Queries;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Templates.Queries;

[TestFixture]
public class ListTemplateRevisionsQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(Template[] templates, TemplateRevision[] revisions)
    {
        A.CallTo(() => _queryDbContext.Set<Template>()).Returns(templates.AsQueryable());
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<Template>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Template> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));

        A.CallTo(() => _queryDbContext.Set<TemplateRevision>()).Returns(revisions.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<TemplateRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<TemplateRevision> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TemplateDoesNotExist()
    {
        Seed([], []);

        var result = await new ListTemplateRevisionsQueryHandler(_queryDbContext)
            .Handle(new ListTemplateRevisionsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnAllRevisions_NewestFirst()
    {
        var authorId = Guid.NewGuid();
        var template = new Template(Guid.NewGuid(), "ADR Template");
        var v1 = template.CreateRevision(null, null, "# v1", authorId);
        var v2 = template.CreateRevision(v1.Id, BumpType.Minor, "# v2", authorId);
        Seed([template], [v1, v2]);

        var result = await new ListTemplateRevisionsQueryHandler(_queryDbContext)
            .Handle(new ListTemplateRevisionsQuery(template.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(2));
            Assert.That(result.Value!.First().Id, Is.EqualTo(v2.Id), "newest revision should be first");
            Assert.That(result.Value!.Last().Id, Is.EqualTo(v1.Id));
        }
    }

    [Test]
    public async Task Handle_Should_OnlyReturnRevisionsForTheRequestedTemplate()
    {
        var authorId = Guid.NewGuid();
        var template = new Template(Guid.NewGuid(), "ADR Template");
        var ownRevision = template.CreateRevision(null, null, "# v1", authorId);

        var otherTemplate = new Template(Guid.NewGuid(), "Other Template");
        var otherRevision = otherTemplate.CreateRevision(null, null, "# other v1", authorId);

        Seed([template, otherTemplate], [ownRevision, otherRevision]);

        var result = await new ListTemplateRevisionsQueryHandler(_queryDbContext)
            .Handle(new ListTemplateRevisionsQuery(template.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(1));
            Assert.That(result.Value!.Single().Id, Is.EqualTo(ownRevision.Id));
        }
    }
}
