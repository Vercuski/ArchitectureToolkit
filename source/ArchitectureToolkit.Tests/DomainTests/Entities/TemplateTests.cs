using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class TemplateTests
{
    [Test]
    public void Constructor_Should_SetCategoryIdAndName()
    {
        var categoryId = Guid.NewGuid();

        var template = new Template(categoryId, "Architecture Vision");

        Assert.That(template.CategoryId, Is.EqualTo(categoryId));
        Assert.That(template.Name, Is.EqualTo("Architecture Vision"));
        Assert.That(template.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(template.CurrentRevisionId, Is.Null);
        Assert.That(template.CurrentVersion, Is.Null);
        Assert.That(template.Revisions, Is.Empty);
    }

    [Test]
    public void Constructor_Should_Throw_When_CategoryIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new Template(Guid.Empty, "Architecture Vision"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_NameIsMissing(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Template(Guid.NewGuid(), name!));
    }

    [Test]
    public void CreateRevision_First_Should_BeSeededAt_1_0_0()
    {
        var template = new Template(Guid.NewGuid(), "Architecture Vision");
        var authorId = Guid.NewGuid();

        var revision = template.CreateRevision(null, null, "initial content", authorId);

        Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
        Assert.That(revision.TemplateId, Is.EqualTo(template.Id));
        Assert.That(revision.Content, Is.EqualTo("initial content"));
        Assert.That(revision.AuthorId, Is.EqualTo(authorId));
        Assert.That(template.CurrentRevisionId, Is.EqualTo(revision.Id));
        Assert.That(template.CurrentVersion, Is.EqualTo(VersionNumber.Initial));
        Assert.That(template.Revisions, Has.Count.EqualTo(1));
        Assert.That(template.Revisions, Does.Contain(revision));
    }

    [Test]
    public void CreateRevision_First_Should_IgnoreBumpType_ForVersioning_ButStillRecordItVerbatim()
    {
        // NOTE: this documents an observed discrepancy, not a designed
        // behavior. TemplateRevision.BumpType's own doc comment says it's
        // "Null only for a Template's very first revision" — but nothing in
        // Template.CreateRevision actually nulls out a caller-supplied
        // bumpType on the first call; RevisionHistory<T> only ignores it for
        // *version calculation*, and the factory closure captures the raw
        // bumpType parameter regardless. If a caller passes a non-null
        // bumpType on the very first revision, it's stored on the resulting
        // TemplateRevision as-is, contradicting that doc comment. Worth a
        // decision: either CreateRevision should force bumpType to null when
        // CurrentRevisionId is null, or the doc comment should be corrected
        // to describe actual behavior.
        var template = new Template(Guid.NewGuid(), "Architecture Vision");

        var revision = template.CreateRevision(null, BumpType.Major, "content", Guid.NewGuid());

        Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
        Assert.That(revision.BumpType, Is.EqualTo(BumpType.Major));
    }

    [Test]
    public void CreateRevision_Should_ThrowRevisionConflictException_When_ExpectedIdDoesNotMatch()
    {
        var template = new Template(Guid.NewGuid(), "Architecture Vision");
        var staleExpectedId = Guid.NewGuid();

        Assert.Throws<RevisionConflictException>(() =>
            template.CreateRevision(staleExpectedId, null, "content", Guid.NewGuid()));
    }

    [Test]
    public void CreateRevision_Second_Should_RequireBumpType()
    {
        var template = new Template(Guid.NewGuid(), "Architecture Vision");
        var first = template.CreateRevision(null, null, "v1", Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() =>
            template.CreateRevision(first.Id, null, "v2", Guid.NewGuid()));
    }

    [Test]
    public void CreateRevision_Second_Should_BumpFromCurrentVersion_And_AppendToRevisions()
    {
        var template = new Template(Guid.NewGuid(), "Architecture Vision");
        var first = template.CreateRevision(null, null, "v1", Guid.NewGuid());

        var second = template.CreateRevision(first.Id, BumpType.Minor, "v2", Guid.NewGuid());

        Assert.That(second.Version, Is.EqualTo(new VersionNumber(1, 1, 0)));
        Assert.That(template.CurrentRevisionId, Is.EqualTo(second.Id));
        Assert.That(template.CurrentVersion, Is.EqualTo(new VersionNumber(1, 1, 0)));
        Assert.That(template.Revisions, Has.Count.EqualTo(2));
    }

    [Test]
    public void CreateRevision_Should_Throw_When_ContentIsMissing()
    {
        var template = new Template(Guid.NewGuid(), "Architecture Vision");

        Assert.Throws<ArgumentException>(() =>
            template.CreateRevision(null, null, "   ", Guid.NewGuid()));
    }

    [Test]
    public void CreateRevision_Should_Throw_When_AuthorIdIsEmpty()
    {
        var template = new Template(Guid.NewGuid(), "Architecture Vision");

        Assert.Throws<ArgumentException>(() =>
            template.CreateRevision(null, null, "content", Guid.Empty));
    }
}
