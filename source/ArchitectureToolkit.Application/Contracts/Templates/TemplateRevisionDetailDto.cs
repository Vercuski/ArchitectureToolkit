namespace ArchitectureToolkit.Application.Contracts.Templates;

/// <summary>Full detail for one historical revision, including its content.</summary>
public sealed record TemplateRevisionDetailDto(
    Guid Id, Guid TemplateId, string Version, string? BumpType, Guid AuthorId, DateTime CreatedAt, string Content);
