using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.Entities;

[TestFixture]
public class ProjectDocumentTests
{
    [Test]
    public void Constructor_Should_SetRequiredFields_And_DefaultSourceTemplateRevisionIdToNull()
    {
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var document = new ProjectDocument(projectId, categoryId, "Architecture Vision");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(document.ProjectId, Is.EqualTo(projectId));
            Assert.That(document.CategoryId, Is.EqualTo(categoryId));
            Assert.That(document.Title, Is.EqualTo("Architecture Vision"));
            Assert.That(document.SourceTemplateRevisionId, Is.Null);
            Assert.That(document.CurrentRevisionId, Is.Null);
            Assert.That(document.CurrentVersion, Is.Null);
            Assert.That(document.Revisions, Is.Empty);
        }
    }

    [Test]
    public void Constructor_Should_SetSourceTemplateRevisionId_WhenProvided()
    {
        var sourceRevisionId = Guid.NewGuid();

        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision", sourceRevisionId);

        Assert.That(document.SourceTemplateRevisionId, Is.EqualTo(sourceRevisionId));
    }

    [Test]
    public void Constructor_Should_Throw_When_ProjectIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ProjectDocument(Guid.Empty, Guid.NewGuid(), "Title"));
    }

    [Test]
    public void Constructor_Should_Throw_When_CategoryIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new ProjectDocument(Guid.NewGuid(), Guid.Empty, "Title"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Constructor_Should_Throw_When_TitleIsMissing(string? title)
    {
        Assert.Throws<ArgumentException>(() => new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), title!));
    }

    [Test]
    public void CreateRevision_First_Should_BeSeededAt_1_0_0()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");
        var authorId = Guid.NewGuid();

        var revision = document.CreateRevision(null, null, "initial content", authorId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
            Assert.That(revision.DocumentId, Is.EqualTo(document.Id));
            Assert.That(revision.Content, Is.EqualTo("initial content"));
            Assert.That(revision.AuthorId, Is.EqualTo(authorId));
            Assert.That(document.CurrentRevisionId, Is.EqualTo(revision.Id));
            Assert.That(document.CurrentVersion, Is.EqualTo(VersionNumber.Initial));
            Assert.That(document.Revisions, Has.Count.EqualTo(1));
            Assert.That(document.Revisions, Does.Contain(revision));
        }
    }

    [Test]
    public void CreateRevision_First_Should_ResolveBumpTypeToNull_EvenIfCallerSuppliesOne()
    {
        // Same fix as Template.CreateRevision (see TemplateTests): a
        // caller-supplied bumpType on the very first CreateRevision call
        // now resolves to null, matching DocumentRevision.BumpType's own
        // doc comment, instead of being stored verbatim.
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");

        var revision = document.CreateRevision(null, BumpType.Major, "content", Guid.NewGuid());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
            Assert.That(revision.BumpType, Is.Null);
        }
    }

    [Test]
    public void CreateRevision_Should_ThrowRevisionConflictException_When_ExpectedIdDoesNotMatch()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");
        var staleExpectedId = Guid.NewGuid();

        Assert.Throws<RevisionConflictException>(() =>
            document.CreateRevision(staleExpectedId, null, "content", Guid.NewGuid()));
    }

    [Test]
    public void CreateRevision_Second_Should_RequireBumpType()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");
        var first = document.CreateRevision(null, null, "v1", Guid.NewGuid());

        Assert.Throws<ArgumentNullException>(() =>
            document.CreateRevision(first.Id, null, "v2", Guid.NewGuid()));
    }

    [Test]
    public void CreateRevision_Second_Should_BumpFromCurrentVersion_And_AppendToRevisions()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");
        var first = document.CreateRevision(null, null, "v1", Guid.NewGuid());

        var second = document.CreateRevision(first.Id, BumpType.Patch, "v2", Guid.NewGuid());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(second.Version, Is.EqualTo(new VersionNumber(1, 0, 1)));
            Assert.That(document.CurrentRevisionId, Is.EqualTo(second.Id));
            Assert.That(document.Revisions, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public void CreateRevision_Should_Throw_When_ContentIsMissing()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");

        Assert.Throws<ArgumentException>(() =>
            document.CreateRevision(null, null, "", Guid.NewGuid()));
    }

    [Test]
    public void CreateRevision_Should_Throw_When_AuthorIdIsEmpty()
    {
        var document = new ProjectDocument(Guid.NewGuid(), Guid.NewGuid(), "Architecture Vision");

        Assert.Throws<ArgumentException>(() =>
            document.CreateRevision(null, null, "content", Guid.Empty));
    }
}
