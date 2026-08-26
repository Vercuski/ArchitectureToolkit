namespace ArchitectureToolkit.Application.Contracts.Templates;

/// <summary>Full template detail, including its current revision's content.</summary>
public sealed record TemplateDetailDto(
    Guid Id, Guid CategoryId, string Name, string CurrentVersion, Guid CurrentRevisionId, string Content);
