namespace ArchitectureToolkit.Application.Contracts.Attachments;

/// <summary>
/// Deliberately excludes StorageKey — see DocumentAttachment's own doc
/// comment on why it's opaque to every caller except IAttachmentStorage.
/// The client builds a download link from Id alone
/// (~/api/projects/{projectId}/attachments/{id}/download); ContentType is
/// included so the editor composable can decide image-vs-link markdown
/// syntax without re-deriving it from FileName's extension.
/// </summary>
public sealed record DocumentAttachmentDto(
    Guid Id, Guid ProjectId, string FileName, string ContentType, long SizeBytes, DateTime UploadedAt);
