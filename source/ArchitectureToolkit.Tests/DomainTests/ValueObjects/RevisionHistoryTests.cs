using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.ValueObjects;

[TestFixture]
public class RevisionHistoryTests
{
    private static FakeRevision Factory(VersionNumber version, BumpType? bumpType, string content, Guid authorId) =>
        new(version, bumpType, content, authorId);

    [Test]
    public void Constructor_Should_Allow_BothNull()
    {
        var history = new RevisionHistory<FakeRevision>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(history.CurrentRevisionId, Is.Null);
            Assert.That(history.CurrentVersion, Is.Null);
        }
    }

    [Test]
    public void Constructor_Should_Allow_BothSet()
    {
        var revisionId = Guid.NewGuid();
        var version = new VersionNumber(1, 2, 3);

        var history = new RevisionHistory<FakeRevision>(revisionId, version);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(history.CurrentRevisionId, Is.EqualTo(revisionId));
            Assert.That(history.CurrentVersion, Is.EqualTo(version));
        }
    }

    [Test]
    public void Constructor_Should_Throw_When_OnlyRevisionIdIsSet()
    {
        Assert.Throws<ArgumentException>(() => new RevisionHistory<FakeRevision>(Guid.NewGuid(), null));
    }

    [Test]
    public void Constructor_Should_Throw_When_OnlyVersionIsSet()
    {
        Assert.Throws<ArgumentException>(() => new RevisionHistory<FakeRevision>(null, new VersionNumber(1, 0, 0)));
    }

    [Test]
    public void AppendRevision_Should_Throw_When_FactoryIsNull()
    {
        var history = new RevisionHistory<FakeRevision>();

        Assert.Throws<ArgumentNullException>(() =>
            history.AppendRevision(null, null, "content", Guid.NewGuid(), null!));
    }

    [Test]
    public void AppendRevision_FirstRevision_Should_AlwaysBeSeededAt_1_0_0()
    {
        var history = new RevisionHistory<FakeRevision>();
        var authorId = Guid.NewGuid();

        var revision = history.AppendRevision(null, null, "content", authorId, Factory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
            Assert.That(history.CurrentVersion, Is.EqualTo(VersionNumber.Initial));
            Assert.That(history.CurrentRevisionId, Is.EqualTo(revision.Id));
        }
    }

    [Test]
    public void AppendRevision_FirstRevision_Should_ResolveBumpTypeToNull_EvenIfCallerSuppliesOne()
    {
        // ADR-0013: the very first revision is always 1.0.0, and nothing was
        // actually bumped to get there — so the *resolved* bump type passed
        // to the factory must be null too, regardless of what the caller
        // passes in. (Previously this wasn't enforced here: the raw
        // parameter reached each aggregate's factory closure unchanged, so
        // a caller-supplied bumpType could end up stored on the first
        // TemplateRevision/DocumentRevision despite being ignored for
        // versioning. Fixed by resolving it here, once, for every caller.)
        var history = new RevisionHistory<FakeRevision>();

        var revision = history.AppendRevision(null, BumpType.Major, "content", Guid.NewGuid(), Factory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
            Assert.That(revision.BumpType, Is.Null);
        }
    }

    [Test]
    public void AppendRevision_Should_ThrowRevisionConflictException_When_ExpectedIdDoesNotMatch_OnFirstCall()
    {
        var history = new RevisionHistory<FakeRevision>();
        var staleExpectedId = Guid.NewGuid();

        var ex = Assert.Throws<RevisionConflictException>(() =>
            history.AppendRevision(staleExpectedId, null, "content", Guid.NewGuid(), Factory));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.ExpectedRevisionId, Is.EqualTo(staleExpectedId));
            Assert.That(ex.ActualRevisionId, Is.Null);
        }
    }

    [Test]
    public void AppendRevision_SecondRevision_Should_RequireBumpType()
    {
        var history = new RevisionHistory<FakeRevision>();
        var first = history.AppendRevision(null, null, "v1", Guid.NewGuid(), Factory);

        Assert.Throws<ArgumentNullException>(() =>
            history.AppendRevision(first.Id, null, "v2", Guid.NewGuid(), Factory));
    }

    [TestCase(BumpType.Major, 2, 0, 0)]
    [TestCase(BumpType.Minor, 1, 6, 0)]
    [TestCase(BumpType.Patch, 1, 5, 10)]
    public void AppendRevision_SecondRevision_Should_BumpFromCurrentVersion(
        BumpType bumpType, int expectedMajor, int expectedMinor, int expectedPatch)
    {
        // Seeded directly at 1.5.9, as if reconstructed from persisted state,
        // to isolate the bump math from append-sequencing (already covered
        // by AppendRevision_Should_UpdateCurrentRevisionIdAndVersion_AfterEachAppend).
        var history = new RevisionHistory<FakeRevision>(Guid.NewGuid(), new VersionNumber(1, 5, 9));

        var revision = history.AppendRevision(
            history.CurrentRevisionId, bumpType, "content", Guid.NewGuid(), Factory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(revision.Version, Is.EqualTo(new VersionNumber(expectedMajor, expectedMinor, expectedPatch)));
            Assert.That(revision.BumpType, Is.EqualTo(bumpType));
        }
    }

    [Test]
    public void AppendRevision_Should_ThrowRevisionConflictException_When_ExpectedIdDoesNotMatch_OnSecondCall()
    {
        var history = new RevisionHistory<FakeRevision>();
        var first = history.AppendRevision(null, null, "v1", Guid.NewGuid(), Factory);
        var staleExpectedId = Guid.NewGuid();

        var ex = Assert.Throws<RevisionConflictException>(() =>
            history.AppendRevision(staleExpectedId, BumpType.Patch, "v2", Guid.NewGuid(), Factory));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex!.ExpectedRevisionId, Is.EqualTo(staleExpectedId));
            Assert.That(ex.ActualRevisionId, Is.EqualTo(first.Id));
        }
    }

    [Test]
    public void AppendRevision_Should_UpdateCurrentRevisionIdAndVersion_AfterEachAppend()
    {
        var history = new RevisionHistory<FakeRevision>();

        var first = history.AppendRevision(null, null, "v1", Guid.NewGuid(), Factory);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(history.CurrentRevisionId, Is.EqualTo(first.Id));
            Assert.That(history.CurrentVersion, Is.EqualTo(VersionNumber.Initial));
        }

        var second = history.AppendRevision(first.Id, BumpType.Minor, "v2", Guid.NewGuid(), Factory);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(history.CurrentRevisionId, Is.EqualTo(second.Id));
            Assert.That(history.CurrentVersion, Is.EqualTo(new VersionNumber(1, 1, 0)));
        }
    }

    [Test]
    public void AppendRevision_Should_PassThroughContentAndAuthorId_ToFactory()
    {
        var history = new RevisionHistory<FakeRevision>();
        var authorId = Guid.NewGuid();

        var revision = history.AppendRevision(null, null, "specific content", authorId, Factory);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(revision.Content, Is.EqualTo("specific content"));
            Assert.That(revision.AuthorId, Is.EqualTo(authorId));
        }
    }
}
