namespace ArchitectureToolkit.Application.Contracts.ProjectDocuments;

/// <summary>Full document detail, including its current revision's content.</summary>
public sealed record ProjectDocumentDetailDto(
    Guid Id,
    Guid ProjectId,
    Guid CategoryId,
    string Title,
    string CurrentVersion,
    Guid CurrentRevisionId,
    Guid? SourceTemplateRevisionId,
    string Content);
