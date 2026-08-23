using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Tests.DomainTests.ValueObjects;

[TestFixture]
public class VersionNumberTests
{
    [Test]
    public void Constructor_Should_SetMajorMinorPatch()
    {
        var version = new VersionNumber(1, 2, 3);

        Assert.That(version.Major, Is.EqualTo(1));
        Assert.That(version.Minor, Is.EqualTo(2));
        Assert.That(version.Patch, Is.EqualTo(3));
    }

    [Test]
    public void Constructor_Should_Allow_AllZeroComponents()
    {
        var version = new VersionNumber(0, 0, 0);

        Assert.That(version, Is.EqualTo(new VersionNumber(0, 0, 0)));
    }

    [TestCase(-1, 0, 0)]
    [TestCase(0, -1, 0)]
    [TestCase(0, 0, -1)]
    public void Constructor_Should_Throw_When_AnyComponentIsNegative(int major, int minor, int patch)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new VersionNumber(major, minor, patch));
    }

    [Test]
    public void Initial_Should_Be_1_0_0()
    {
        // ADR-0013: every template/document is seeded at 1.0.0.
        Assert.That(VersionNumber.Initial, Is.EqualTo(new VersionNumber(1, 0, 0)));
    }

    [Test]
    public void Bump_Major_Should_IncrementMajor_And_ResetMinorAndPatch()
    {
        var version = new VersionNumber(1, 5, 9);

        var bumped = version.Bump(BumpType.Major);

        Assert.That(bumped, Is.EqualTo(new VersionNumber(2, 0, 0)));
    }

    [Test]
    public void Bump_Minor_Should_IncrementMinor_And_ResetPatch_ButNotMajor()
    {
        var version = new VersionNumber(1, 5, 9);

        var bumped = version.Bump(BumpType.Minor);

        Assert.That(bumped, Is.EqualTo(new VersionNumber(1, 6, 0)));
    }

    [Test]
    public void Bump_Patch_Should_IncrementPatch_ButNotMajorOrMinor()
    {
        var version = new VersionNumber(1, 5, 9);

        var bumped = version.Bump(BumpType.Patch);

        Assert.That(bumped, Is.EqualTo(new VersionNumber(1, 5, 10)));
    }

    [Test]
    public void Bump_Should_Throw_When_BumpTypeIsUndefined()
    {
        var version = new VersionNumber(1, 0, 0);
        var undefinedBumpType = (BumpType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => version.Bump(undefinedBumpType));
    }

    [TestCase("1.0.0", 1, 0, 0)]
    [TestCase("0.0.0", 0, 0, 0)]
    [TestCase("10.20.30", 10, 20, 30)]
    public void TryParse_Should_ReturnTrue_And_ParseCorrectly_ForValidInput(
        string input, int expectedMajor, int expectedMinor, int expectedPatch)
    {
        var success = VersionNumber.TryParse(input, out var result);

        Assert.That(success, Is.True);
        Assert.That(result, Is.EqualTo(new VersionNumber(expectedMajor, expectedMinor, expectedPatch)));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("1.0")]
    [TestCase("1.0.0.0")]
    [TestCase("a.b.c")]
    [TestCase("1.x.0")]
    [TestCase("-1.0.0")]
    [TestCase("1.-1.0")]
    [TestCase("1.0.-1")]
    [TestCase("1..0")]
    public void TryParse_Should_ReturnFalse_ForMalformedInput(string? input)
    {
        var success = VersionNumber.TryParse(input, out var result);

        Assert.That(success, Is.False);
        Assert.That(result, Is.EqualTo(default(VersionNumber)));
    }

    [Test]
    public void Parse_Should_ReturnCorrectValue_ForValidInput()
    {
        var result = VersionNumber.Parse("2.4.6");

        Assert.That(result, Is.EqualTo(new VersionNumber(2, 4, 6)));
    }

    [Test]
    public void Parse_Should_ThrowFormatException_ForMalformedInput()
    {
        var ex = Assert.Throws<FormatException>(() => VersionNumber.Parse("not-a-version"));

        Assert.That(ex!.Message, Does.Contain("not-a-version"));
    }

    [Test]
    public void ToString_Should_Return_MajorDotMinorDotPatch()
    {
        var version = new VersionNumber(3, 2, 1);

        Assert.That(version.ToString(), Is.EqualTo("3.2.1"));
    }

    [TestCase(0, 0, 0)]
    [TestCase(1, 0, 0)]
    [TestCase(12, 34, 56)]
    public void ToString_Then_Parse_Should_RoundTrip(int major, int minor, int patch)
    {
        var original = new VersionNumber(major, minor, patch);

        var roundTripped = VersionNumber.Parse(original.ToString());

        Assert.That(roundTripped, Is.EqualTo(original));
    }

    [Test]
    public void Equality_Should_Be_ValueBased()
    {
        // VersionNumber is a readonly record struct — this locks in that its
        // auto-generated equality stays value-based, since RevisionHistory<T>
        // and the seeded-at-1.0.0 rule both depend on VersionNumber comparing
        // by value rather than reference.
        var a = new VersionNumber(1, 2, 3);
        var b = new VersionNumber(1, 2, 3);
        var c = new VersionNumber(1, 2, 4);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a == b, Is.True);
        Assert.That(a, Is.Not.EqualTo(c));
        Assert.That(a == c, Is.False);
    }
}
