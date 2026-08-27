namespace ArchitectureToolkit.Application.Contracts.ProjectDocuments;

/// <summary>
/// Lightweight metadata for browsing a project's documents — intentionally
/// excludes Content (see ProjectDocumentDetailDto), matching
/// TemplateSummaryDto's rationale.
/// </summary>
public sealed record ProjectDocumentSummaryDto(
    Guid Id, Guid ProjectId, Guid CategoryId, string Title, string CurrentVersion);
