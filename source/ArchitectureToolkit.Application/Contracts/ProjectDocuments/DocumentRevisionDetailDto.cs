namespace ArchitectureToolkit.Application.Contracts.ProjectDocuments;

/// <summary>Full detail for one historical revision, including its content.</summary>
public sealed record DocumentRevisionDetailDto(
    Guid Id, Guid DocumentId, string Version, string? BumpType, Guid AuthorId, DateTime CreatedAt, string Content);
