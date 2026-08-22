namespace ArchitectureToolkit.Domain.ValueObjects;

/// <summary>
/// The SemVer bump captured by the save popup (Domain Data Model.md §3:
/// "SemVer bump is a domain decision"). Persisted as TEMPLATE_REVISION/
/// DOCUMENT_REVISION's `bump_type` column.
/// </summary>
public enum BumpType
{
    Major,
    Minor,
    Patch
}
