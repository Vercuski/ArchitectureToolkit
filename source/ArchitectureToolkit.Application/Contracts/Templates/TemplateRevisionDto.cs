namespace ArchitectureToolkit.Application.Contracts.Templates;

public sealed record TemplateRevisionDto(
    Guid Id, Guid TemplateId, string Version, string? BumpType, Guid AuthorId, DateTime CreatedAt);
