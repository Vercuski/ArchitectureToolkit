using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

public sealed record CreateProjectDocumentRequest(
    Guid CategoryId, string Title, Guid? SourceTemplateRevisionId, string Content);

public sealed record CreateDocumentRevisionRequest(Guid? ExpectedCurrentRevisionId, BumpType? BumpType, string Content);
