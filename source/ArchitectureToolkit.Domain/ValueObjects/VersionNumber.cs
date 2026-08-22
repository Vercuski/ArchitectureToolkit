namespace ArchitectureToolkit.Domain.ValueObjects;

/// <summary>
/// A Major.Minor.Patch SemVer value, with the reset rules Domain Data Model.md §3
/// specifies: a major bump resets minor and patch to 0; a minor bump resets patch to 0.
/// </summary>
public readonly record struct VersionNumber
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    public VersionNumber(int major, int minor, int patch)
    {
        if (major < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), major, "Major version cannot be negative.");
        }
        if (minor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minor), minor, "Minor version cannot be negative.");
        }
        if (patch < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(patch), patch, "Patch version cannot be negative.");
        }

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// The version every template/document is seeded at on first creation (ADR-0013).
    /// </summary>
    public static VersionNumber Initial => new(1, 0, 0);

    /// <summary>
    /// Applies a SemVer bump, resetting lower-order components per the rules above.
    /// </summary>
    public VersionNumber Bump(BumpType bumpType) => bumpType switch
    {
        BumpType.Major => new VersionNumber(Major + 1, 0, 0),
        BumpType.Minor => new VersionNumber(Major, Minor + 1, 0),
        BumpType.Patch => new VersionNumber(Major, Minor, Patch + 1),
        _ => throw new ArgumentOutOfRangeException(nameof(bumpType), bumpType, null)
    };

    public static VersionNumber Parse(string value)
    {
        if (!TryParse(value, out var result))
        {
            throw new FormatException(
                $"'{value}' is not a valid VersionNumber. Expected format: Major.Minor.Patch (e.g. '1.0.0').");
        }
        return result;
    }

    public static bool TryParse(string? value, out VersionNumber result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var major) || major < 0)
        {
            return false;
        }
        if (!int.TryParse(parts[1], out var minor) || minor < 0)
        {
            return false;
        }
        if (!int.TryParse(parts[2], out var patch) || patch < 0)
        {
            return false;
        }

        result = new VersionNumber(major, minor, patch);
        return true;
    }

    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
