using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Templates.Queries;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Templates.Queries;

[TestFixture]
public class ListTemplatesQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(Template[] templates)
    {
        A.CallTo(() => _queryDbContext.Set<Template>()).Returns(templates.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<Template>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Template> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnAllTemplates_WithVersion()
    {
        var categoryId = Guid.NewGuid();
        var authorId = Guid.NewGuid();

        var t1 = new Template(categoryId, "ADR Template");
        t1.CreateRevision(null, null, "# ADR", authorId);

        var t2 = new Template(categoryId, "Vision Template");
        t2.CreateRevision(null, null, "# Vision", authorId);

        Seed([t1, t2]);

        var result = await new ListTemplatesQueryHandler(_queryDbContext)
            .Handle(new ListTemplatesQuery(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Has.Count.EqualTo(2));
            Assert.That(result.Value!.Any(t => t.Name == "ADR Template" && t.CurrentVersion == "1.0.0"), Is.True);
        }
    }

    [Test]
    public async Task Handle_Should_ExcludeTemplates_WithNoRevisionsYet()
    {
        var bareTemplate = new Template(Guid.NewGuid(), "No Revisions Yet");
        Seed([bareTemplate]);

        var result = await new ListTemplatesQueryHandler(_queryDbContext)
            .Handle(new ListTemplatesQuery(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Empty);
        }
    }

    [Test]
    public async Task Handle_Should_ReturnEmpty_When_NoTemplatesExist()
    {
        Seed([]);

        var result = await new ListTemplatesQueryHandler(_queryDbContext)
            .Handle(new ListTemplatesQuery(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Empty);
        }
    }
}
