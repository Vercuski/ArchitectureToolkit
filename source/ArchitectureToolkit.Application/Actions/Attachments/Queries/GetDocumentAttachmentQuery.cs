using ArchitectureToolkit.Application.Abstractions;

namespace ArchitectureToolkit.Application.Actions.Attachments.Queries;

/// <summary>
/// Authorized to any project member, including Viewer — unlike upload,
/// downloading is read access to content the member can already see
/// rendered in the document itself.
/// </summary>
public sealed record GetDocumentAttachmentQuery(Guid CallerUserId, Guid ProjectId, Guid AttachmentId)
    : IMediatRQueryRequest<Result<DocumentAttachmentContent>>;

/// <summary>
/// The download-specific shape — distinct from DocumentAttachmentDto
/// (Contracts/Attachments), which is what upload's response and any future
/// "list this project's attachments" endpoint return. This carries the
/// open Content stream itself, which DocumentAttachmentDto deliberately
/// never does.
/// </summary>
public sealed record DocumentAttachmentContent(string FileName, string ContentType, Stream Content);

