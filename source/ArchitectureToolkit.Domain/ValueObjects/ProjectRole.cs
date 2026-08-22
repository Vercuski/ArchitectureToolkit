namespace ArchitectureToolkit.Domain.ValueObjects;

/// <summary>
/// A User's permission level on a specific Project (Domain Data Model.md §2).
/// Governs PROJECT_DOCUMENT/DOCUMENT_REVISION access — entirely independent
/// of SystemRole, which only governs the shared template library (ADR-0006).
/// </summary>
public enum ProjectRole
{
    Viewer,
    Editor,
    Owner
}
