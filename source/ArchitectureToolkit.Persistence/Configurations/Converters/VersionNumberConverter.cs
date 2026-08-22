using ArchitectureToolkit.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArchitectureToolkit.Persistence.Configurations.Converters;

/// <summary>
/// Maps VersionNumber to/from its "Major.Minor.Patch" string representation.
/// Shared by every non-nullable Version column (TemplateRevision.Version,
/// DocumentRevision.Version).
/// </summary>
public sealed class VersionNumberConverter : ValueConverter<VersionNumber, string>
{
    public VersionNumberConverter()
        : base(
            v => v.ToString(),
            v => VersionNumber.Parse(v))
    {
    }
}
