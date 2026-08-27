namespace ArchitectureToolkit.Application.Contracts.ProjectDocuments;

public sealed record DocumentRevisionDto(
    Guid Id, Guid DocumentId, string Version, string? BumpType, Guid AuthorId, DateTime CreatedAt);
