using ArchitectureToolkit.Application.Abstractions;

namespace ArchitectureToolkit.Application.Actions.Projects.Queries;

/// <summary>
/// Builds a full project export — a zip containing a master.pdf
/// (cover/TOC/contributors/links) plus one PDF per current document
/// revision under documents/. Authorized to any project member,
/// including Viewer — this is read access to content the caller can
/// already see in the app, same reasoning as GetDocumentAttachmentQuery.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record ExportProjectQuery(Guid CallerUserId, Guid ProjectId)
    : IMediatRQueryRequest<Result<ProjectExportArchive>>;

/// <summary>
/// The zip's content stream plus a suggested file name — mirrors
/// DocumentAttachmentContent's shape for the same reason: this carries
/// an open stream, which nothing else in Contracts/Projects ever does.
/// </summary>
public sealed record ProjectExportArchive(Stream Content, string FileName);
