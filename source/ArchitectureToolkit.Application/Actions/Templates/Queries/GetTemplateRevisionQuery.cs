using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Templates;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

/// <param name="TemplateId">
/// Included for URL-nesting/scoping consistency and validated against the
/// revision — a revisionId that exists but belongs to a different template
/// returns NotFound rather than leaking cross-template content.
/// </param>
public sealed record GetTemplateRevisionQuery(Guid TemplateId, Guid RevisionId)
    : IMediatRQueryRequest<Result<TemplateRevisionDetailDto>>;
