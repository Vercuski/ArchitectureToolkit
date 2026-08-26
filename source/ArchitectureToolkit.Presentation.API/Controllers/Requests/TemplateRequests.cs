using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

public sealed record CreateTemplateRequest(Guid CategoryId, string Name, string Content);

public sealed record CreateTemplateRevisionRequest(Guid? ExpectedCurrentRevisionId, BumpType? BumpType, string Content);
