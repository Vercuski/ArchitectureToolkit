using ArchitectureToolkit.Domain.Exceptions;

namespace ArchitectureToolkit.Tests.DomainTests.Exceptions;

[TestFixture]
public class RevisionConflictExceptionTests
{
    [Test]
    public void Constructor_Should_SetExpectedAndActualRevisionIds()
    {
        var expected = Guid.NewGuid();
        var actual = Guid.NewGuid();

        var ex = new RevisionConflictException(expected, actual);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.ExpectedRevisionId, Is.EqualTo(expected));
            Assert.That(ex.ActualRevisionId, Is.EqualTo(actual));
        }
    }

    [Test]
    public void Message_Should_IncludeBothRevisionIds_When_BothAreSet()
    {
        var expected = Guid.NewGuid();
        var actual = Guid.NewGuid();

        var ex = new RevisionConflictException(expected, actual);

        Assert.That(ex.Message, Does.Contain(expected.ToString()));
        Assert.That(ex.Message, Does.Contain(actual.ToString()));
    }

    [Test]
    public void Message_Should_SayNone_When_ExpectedRevisionIdIsNull()
    {
        var actual = Guid.NewGuid();

        var ex = new RevisionConflictException(null, actual);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.ExpectedRevisionId, Is.Null);
            Assert.That(ex.Message, Does.Contain("(none)"));
            Assert.That(ex.Message, Does.Contain(actual.ToString()));
        }
    }

    [Test]
    public void Message_Should_SayNone_When_ActualRevisionIdIsNull()
    {
        var expected = Guid.NewGuid();

        var ex = new RevisionConflictException(expected, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.ActualRevisionId, Is.Null);
            Assert.That(ex.Message, Does.Contain(expected.ToString()));
            Assert.That(ex.Message, Does.Contain("(none)"));
        }
    }

    [Test]
    public void Message_Should_SayNoneTwice_When_BothRevisionIdsAreNull()
    {
        var ex = new RevisionConflictException(null, null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ex.ExpectedRevisionId, Is.Null);
            Assert.That(ex.ActualRevisionId, Is.Null);
        }
        // Both placeholders render as "(none)" — confirm the message doesn't
        // silently collapse or omit either half when both sides are absent.
        Assert.That(ex.Message.Split("(none)").Length - 1, Is.EqualTo(2));
    }
}
