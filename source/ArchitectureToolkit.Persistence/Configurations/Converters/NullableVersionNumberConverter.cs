using ArchitectureToolkit.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ArchitectureToolkit.Persistence.Configurations.Converters;

/// <summary>
/// Maps nullable VersionNumber to/from its "Major.Minor.Patch" string
/// representation. Shared by every nullable CurrentVersion column
/// (Template.CurrentVersion, ProjectDocument.CurrentVersion) — null means
/// "no revisions yet".
/// </summary>
public sealed class NullableVersionNumberConverter : ValueConverter<VersionNumber?, string?>
{
    public NullableVersionNumberConverter()
        : base(
            v => v.HasValue ? v.Value.ToString() : null,
            v => v == null ? null : VersionNumber.Parse(v))
    {
    }
}
