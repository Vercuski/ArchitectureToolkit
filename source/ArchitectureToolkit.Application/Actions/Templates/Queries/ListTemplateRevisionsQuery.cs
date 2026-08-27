using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Templates;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

/// <summary>
/// Lists a template's revision history — lightweight summaries (no
/// content, matching TemplateSummaryDto's rationale for the same reason);
/// use GetTemplateRevisionQuery for a specific historical revision's full
/// content. Available to any authenticated user, same as browsing the
/// template library itself.
/// </summary>
public sealed record ListTemplateRevisionsQuery(Guid TemplateId)
    : IMediatRQueryRequest<Result<IReadOnlyCollection<TemplateRevisionDto>>>;
