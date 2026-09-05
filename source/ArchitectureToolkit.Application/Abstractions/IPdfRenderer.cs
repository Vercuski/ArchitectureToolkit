namespace ArchitectureToolkit.Application.Abstractions;

/// <summary>
/// Renders the content of a project export (Project PDF Export feature)
/// into PDF bytes. Two renders make up one export: the master document's
/// cover/TOC/contributors/links section, and one call per project
/// document. Deliberately free of any third-party PDF/markdown library
/// type in its signature — every parameter is a plain Application-layer
/// record, so Application itself never needs a PackageReference to
/// whichever rendering library implements this.
///
/// Both methods are synchronous (not Task-returning) on purpose:
/// rendering is CPU-bound, not I/O-bound, unlike IAttachmentStorage/
/// IEmailSender — there's no actual asynchronous work happening under
/// either implementation this interface would be hiding.
///
/// The concrete implementation (QuestPDF, per the PDF Rendering Library
/// Trade Study / ADR-0019) lives in Presentation.API, not Infrastructure
/// or Persistence — same reasoning as IEmailSender: Infrastructure is
/// walled off from Application by an enforced fitness test
/// (InfrastructureArchitectureTests) and so cannot implement an
/// Application interface at all, and Persistence has no reason to know
/// about PDF rendering. Presentation.API is the only project that
/// references both this interface and whatever rendering library
/// implements it.
/// </summary>
public interface IPdfRenderer
{
    /// <summary>
    /// Renders the master document's front matter: a cover page (project
    /// name, export timestamp), a hierarchical table of contents
    /// (category → document titles), a contributors page, and a flat
    /// list of links to every exported document's relative path under
    /// documents/.
    /// </summary>
    byte[] RenderCoverSection(ProjectExportManifest manifest);

    /// <summary>
    /// Renders one project document's current revision as a standalone
    /// PDF — a small header (title, category, version, last updated)
    /// followed by the revision's markdown content.
    /// </summary>
    byte[] RenderMarkdownDocument(ExportedDocumentContent document);
}

/// <summary>
/// Everything RenderCoverSection needs. Categories/Documents arrive
/// already ordered exactly as they should appear — the renderer doesn't
/// re-sort anything.
/// </summary>
public sealed record ProjectExportManifest(
    string ProjectName,
    DateTime ExportedAtUtc,
    IReadOnlyCollection<ProjectExportContributor> Contributors,
    IReadOnlyCollection<ProjectExportCategorySection> Categories);

/// <summary>One row on the contributors page.</summary>
public sealed record ProjectExportContributor(string Name, string Email);

/// <summary>
/// One category's worth of TOC/links entries — grouping is what makes
/// the TOC hierarchical (category header, documents nested under it)
/// rather than a single flat list.
/// </summary>
public sealed record ProjectExportCategorySection(
    string CategoryName,
    IReadOnlyCollection<ProjectExportDocumentEntry> Documents);

/// <summary>
/// One document's entry on the links page. RelativePath is the path
/// inside the export zip (e.g. "documents/02-core-architecture_domain-model.pdf"),
/// rendered as plain text rather than a clickable annotation — a
/// relative-file hyperlink that only resolves after the zip is extracted
/// is unreliable across PDF viewers, so a readable path beats a link
/// that silently fails in some readers.
/// </summary>
public sealed record ProjectExportDocumentEntry(string Title, string RelativePath);

/// <summary>Everything RenderMarkdownDocument needs for one document.</summary>
/// <param name="InlineImages">
/// Every attachment image referenced in MarkdownContent, pre-resolved and
/// keyed by the exact URL string as it appears in the markdown (the
/// "/api/projects/{id}/attachments/{id}/download" form). Resolving these
/// is a database + IAttachmentStorage concern that belongs in the
/// Application handler building this record, not in the renderer, which
/// only ever deals with bytes it's already been handed.
/// </param>
public sealed record ExportedDocumentContent(
    string Title,
    string CategoryName,
    string Version,
    DateTime LastUpdatedAtUtc,
    string MarkdownContent,
    IReadOnlyDictionary<string, ExportedImage> InlineImages);

/// <summary>One resolved image's bytes, ready to embed.</summary>
public sealed record ExportedImage(byte[] Content, string ContentType);
