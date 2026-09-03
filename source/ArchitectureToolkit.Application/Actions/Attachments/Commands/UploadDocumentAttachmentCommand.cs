using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Attachments;

namespace ArchitectureToolkit.Application.Actions.Attachments.Commands;

/// <summary>
/// Authorized identically to CreateProjectDocumentCommand/
/// CreateDocumentRevisionCommand — an Editor or Owner project member,
/// never a Viewer — since uploading is part of composing document
/// content, not a separate permission of its own. See DocumentAttachment's
/// own doc comment for why this is project-scoped rather than
/// document-scoped.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
/// <param name="Content">
/// Disposed by the caller (the controller action, via IFormFile's using
/// scope) once Handle returns — SaveAsync has already fully consumed and
/// persisted it by then.
/// </param>
public sealed record UploadDocumentAttachmentCommand(
    Guid CallerUserId, Guid ProjectId, string FileName, string ContentType, long SizeBytes, Stream Content)
    : IMediatRCommandRequest<Result<DocumentAttachmentDto>>;
