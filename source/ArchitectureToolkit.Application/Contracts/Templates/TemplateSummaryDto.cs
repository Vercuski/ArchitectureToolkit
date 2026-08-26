namespace ArchitectureToolkit.Application.Contracts.Templates;

/// <summary>
/// Lightweight metadata for browsing the template library —
/// intentionally excludes Content (see TemplateDetailDto), since listing
/// the full ~50-template library shouldn't pull every template's full
/// markdown body over the wire.
/// </summary>
public sealed record TemplateSummaryDto(Guid Id, Guid CategoryId, string Name, string CurrentVersion);
