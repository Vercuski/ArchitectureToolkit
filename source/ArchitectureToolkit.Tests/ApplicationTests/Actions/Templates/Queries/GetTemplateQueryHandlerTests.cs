using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Templates.Queries;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Templates.Queries;

[TestFixture]
public class GetTemplateQueryHandlerTests
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
        A.CallTo(() => _queryDbContext.SingleOrDefaultAsync(A<IQueryable<TemplateRevision>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<TemplateRevision> q, CancellationToken _) => Task.FromResult(q.SingleOrDefault()));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TemplateDoesNotExist()
    {
        Seed([], []);

        var result = await new GetTemplateQueryHandler(_queryDbContext)
            .Handle(new GetTemplateQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnNotFound_When_TemplateHasNoRevisionsYet()
    {
        var bareTemplate = new Template(Guid.NewGuid(), "Bare");
        Seed([bareTemplate], []);

        var result = await new GetTemplateQueryHandler(_queryDbContext)
            .Handle(new GetTemplateQuery(bareTemplate.Id), CancellationToken.None);

        Assert.That(result.ErrorType, Is.EqualTo(ResultErrorType.NotFound));
    }

    [Test]
    public async Task Handle_Should_ReturnTemplate_WithCurrentRevisionContent()
    {
        var authorId = Guid.NewGuid();
        var template = new Template(Guid.NewGuid(), "ADR Template");
        var revision = template.CreateRevision(null, null, "# ADR content", authorId);
        Seed([template], [revision]);

        var result = await new GetTemplateQueryHandler(_queryDbContext)
            .Handle(new GetTemplateQuery(template.Id), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Name, Is.EqualTo("ADR Template"));
            Assert.That(result.Value!.Content, Is.EqualTo("# ADR content"));
            Assert.That(result.Value!.CurrentVersion, Is.EqualTo("1.0.0"));
            Assert.That(result.Value!.CurrentRevisionId, Is.EqualTo(revision.Id));
        }
    }
}
