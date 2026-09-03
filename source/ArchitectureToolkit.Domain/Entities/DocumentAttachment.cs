using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// A file uploaded through the document editor (ToastUI's custom "attach
/// file" toolbar button — addImageBlobHook only covers images, so any
/// other file type needs its own upload path) and referenced by a
/// PROJECT_DOCUMENT's markdown content as a link.
///
/// Deliberately scoped to PROJECT rather than to a specific
/// PROJECT_DOCUMENT or DOCUMENT_REVISION:
///
/// 1. Uploading has to work while composing a *brand new* document
///    (CreateDocumentView), before that document has an Id at all — a
///    project's Id is already known at that point (route param), so
///    scoping here avoids a chicken-and-egg problem a document-scoped
///    design couldn't cleanly avoid without a two-phase
///    create-draft-then-attach flow.
/// 2. DOCUMENT_REVISION content is immutable and permanent (Domain Data
///    Model.md §2) — an attachment referenced by an old revision must
///    keep working indefinitely, even after later revisions stop
///    referencing it, so tying an attachment's lifetime to one specific
///    revision would be wrong regardless.
///
/// The consequence, worth being explicit about: nothing currently prunes
/// an attachment once every revision that referenced it has been
/// superseded — same trade-off DOCUMENT_REVISION itself already makes
/// (revisions are never deleted either), not a gap unique to this entity.
/// </summary>
public sealed class DocumentAttachment : Entity
{
    public Guid ProjectId { get; private set; }
    public string FileName { get; private set; }
    public string ContentType { get; private set; }
    public long SizeBytes { get; private set; }

    /// <summary>
    /// Opaque to everything except IAttachmentStorage — the on-disk
    /// location under Attachments:RootPath. Never exposed to the client;
    /// downloads always go through GetDocumentAttachmentQuery, which
    /// re-checks project membership on every request rather than trusting
    /// a path a client could otherwise guess or tamper with.
    /// </summary>
    public string StorageKey { get; private set; }

    public Guid UploadedByUserId { get; private set; }
    public DateTime UploadedAt { get; private set; }

    public DocumentAttachment(
        Guid projectId, string fileName, string contentType, long sizeBytes, string storageKey, Guid uploadedByUserId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("ProjectId is required.", nameof(projectId));
        }
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("FileName is required.", nameof(fileName));
        }
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        }

        ProjectId = projectId;
        FileName = fileName;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
        SizeBytes = sizeBytes;
        StorageKey = storageKey;
        UploadedByUserId = uploadedByUserId;
        UploadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Called once, immediately after construction, by
    /// UploadDocumentAttachmentCommandHandler — the real storage key can
    /// only be known once IAttachmentStorage.SaveAsync has actually
    /// written the file, which itself needs this entity's Id first (the
    /// key is scoped to it), so the constructor is given a placeholder and
    /// this fills in the real value before the entity is ever persisted.
    /// </summary>
    public void SetStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("StorageKey is required.", nameof(storageKey));
        }

        StorageKey = storageKey;
    }
}
