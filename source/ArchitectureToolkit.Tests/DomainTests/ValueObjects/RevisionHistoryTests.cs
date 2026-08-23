using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.ValueObjects;

[TestFixture]
public class RevisionHistoryTests
{
    private static FakeRevision Factory(VersionNumber version, string content, Guid authorId) =>
        new(version, content, authorId);

    [Test]
    public void Constructor_Should_Allow_BothNull()
    {
        var history = new RevisionHistory<FakeRevision>();

        Assert.That(history.CurrentRevisionId, Is.Null);
        Assert.That(history.CurrentVersion, Is.Null);
    }

    [Test]
    public void Constructor_Should_Allow_BothSet()
    {
        var revisionId = Guid.NewGuid();
        var version = new VersionNumber(1, 2, 3);

        var history = new RevisionHistory<FakeRevision>(revisionId, version);

        Assert.That(history.CurrentRevisionId, Is.EqualTo(revisionId));
        Assert.That(history.CurrentVersion, Is.EqualTo(version));
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

        Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
        Assert.That(history.CurrentVersion, Is.EqualTo(VersionNumber.Initial));
        Assert.That(history.CurrentRevisionId, Is.EqualTo(revision.Id));
    }

    [Test]
    public void AppendRevision_FirstRevision_Should_IgnoreBumpType_ForVersioning()
    {
        // ADR-0013: the very first revision is always 1.0.0, regardless of
        // whatever bumpType the caller passes — there is nothing to bump
        // from yet.
        var history = new RevisionHistory<FakeRevision>();

        var revision = history.AppendRevision(null, BumpType.Major, "content", Guid.NewGuid(), Factory);

        Assert.That(revision.Version, Is.EqualTo(VersionNumber.Initial));
    }

    [Test]
    public void AppendRevision_Should_ThrowRevisionConflictException_When_ExpectedIdDoesNotMatch_OnFirstCall()
    {
        var history = new RevisionHistory<FakeRevision>();
        var staleExpectedId = Guid.NewGuid();

        var ex = Assert.Throws<RevisionConflictException>(() =>
            history.AppendRevision(staleExpectedId, null, "content", Guid.NewGuid(), Factory));

        Assert.That(ex!.ExpectedRevisionId, Is.EqualTo(staleExpectedId));
        Assert.That(ex.ActualRevisionId, Is.Null);
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

        Assert.That(revision.Version, Is.EqualTo(new VersionNumber(expectedMajor, expectedMinor, expectedPatch)));
    }

    [Test]
    public void AppendRevision_Should_ThrowRevisionConflictException_When_ExpectedIdDoesNotMatch_OnSecondCall()
    {
        var history = new RevisionHistory<FakeRevision>();
        var first = history.AppendRevision(null, null, "v1", Guid.NewGuid(), Factory);
        var staleExpectedId = Guid.NewGuid();

        var ex = Assert.Throws<RevisionConflictException>(() =>
            history.AppendRevision(staleExpectedId, BumpType.Patch, "v2", Guid.NewGuid(), Factory));

        Assert.That(ex!.ExpectedRevisionId, Is.EqualTo(staleExpectedId));
        Assert.That(ex.ActualRevisionId, Is.EqualTo(first.Id));
    }

    [Test]
    public void AppendRevision_Should_UpdateCurrentRevisionIdAndVersion_AfterEachAppend()
    {
        var history = new RevisionHistory<FakeRevision>();

        var first = history.AppendRevision(null, null, "v1", Guid.NewGuid(), Factory);
        Assert.That(history.CurrentRevisionId, Is.EqualTo(first.Id));
        Assert.That(history.CurrentVersion, Is.EqualTo(VersionNumber.Initial));

        var second = history.AppendRevision(first.Id, BumpType.Minor, "v2", Guid.NewGuid(), Factory);
        Assert.That(history.CurrentRevisionId, Is.EqualTo(second.Id));
        Assert.That(history.CurrentVersion, Is.EqualTo(new VersionNumber(1, 1, 0)));
    }

    [Test]
    public void AppendRevision_Should_PassThroughContentAndAuthorId_ToFactory()
    {
        var history = new RevisionHistory<FakeRevision>();
        var authorId = Guid.NewGuid();

        var revision = history.AppendRevision(null, null, "specific content", authorId, Factory);

        Assert.That(revision.Content, Is.EqualTo("specific content"));
        Assert.That(revision.AuthorId, Is.EqualTo(authorId));
    }
}
