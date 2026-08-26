using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Templates;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

/// <param name="TemplateId">The template to fetch.</param>
public sealed record GetTemplateQuery(Guid TemplateId) : IMediatRQueryRequest<Result<TemplateDetailDto>>;
