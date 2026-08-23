using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class CategoryTests
{
    [Test]
    public void Constructor_Should_SetCodeAndName()
    {
        var category = new Category("02-core-architecture", "Core Architecture");

        Assert.That(category.Code, Is.EqualTo("02-core-architecture"));
        Assert.That(category.Name, Is.EqualTo("Core Architecture"));
        Assert.That(category.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_CodeIsMissing(string? code)
    {
        Assert.Throws<ArgumentException>(() => new Category(code!, "Core Architecture"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_NameIsMissing(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Category("02-core-architecture", name!));
    }
}
