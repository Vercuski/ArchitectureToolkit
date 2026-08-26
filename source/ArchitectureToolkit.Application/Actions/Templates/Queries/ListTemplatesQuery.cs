using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Templates;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

/// <summary>
/// Lists the entire template library — the "GET /api/templates" enhancement
/// flagged during Phase 3, so external AI tools/assistants can reference
/// the full template library programmatically. Available to any
/// authenticated user; unlike creating/versioning templates (ADR-0006),
/// browsing the library isn't restricted to Architects.
/// </summary>
public sealed record ListTemplatesQuery : IMediatRQueryRequest<Result<IReadOnlyCollection<TemplateSummaryDto>>>;
