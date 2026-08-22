namespace ArchitectureToolkit.Domain.ValueObjects;

/// <summary>
/// Gates template-library governance globally, independent of any project
/// (ADR-0006). Only Architect users may create or version a TEMPLATE.
/// Contributor is the default for every user except the first (ADR-0009).
/// </summary>
public enum SystemRole
{
    Contributor,
    Architect
}
