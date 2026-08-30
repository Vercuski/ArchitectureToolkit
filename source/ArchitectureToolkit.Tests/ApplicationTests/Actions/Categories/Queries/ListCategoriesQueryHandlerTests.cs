using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Actions.Categories.Queries;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Tests.ApplicationTests.Actions.Categories.Queries;

[TestFixture]
public class ListCategoriesQueryHandlerTests
{
    private IQueryDbContext _queryDbContext = null!;

    [SetUp]
    public void SetUp()
    {
        _queryDbContext = A.Fake<IQueryDbContext>();
    }

    private void Seed(Category[] categories)
    {
        A.CallTo(() => _queryDbContext.Set<Category>()).Returns(categories.AsQueryable());
        A.CallTo(() => _queryDbContext.ToListAsync(A<IQueryable<Category>>._, A<CancellationToken>._))
            .ReturnsLazily((IQueryable<Category> q, CancellationToken _) => Task.FromResult(q.ToList()));
    }

    [Test]
    public async Task Handle_Should_ReturnCategories_OrderedByCode()
    {
        var vision = new Category("00-vision-and-strategy", "Vision and Strategy");
        var handover = new Category("11-handover", "Handover");
        var core = new Category("02-core-architecture", "Core Architecture");
        Seed([handover, vision, core]);

        var result = await new ListCategoriesQueryHandler(_queryDbContext)
            .Handle(new ListCategoriesQuery(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value!.Select(c => c.Code), Is.EqualTo(
            [
                "00-vision-and-strategy", "02-core-architecture", "11-handover",
            ]));
        }
    }

    [Test]
    public async Task Handle_Should_ReturnEmpty_When_NoCategoriesExist()
    {
        Seed([]);

        var result = await new ListCategoriesQueryHandler(_queryDbContext)
            .Handle(new ListCategoriesQuery(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.Empty);
        }
    }
}
