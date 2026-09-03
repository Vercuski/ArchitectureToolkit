namespace ArchitectureToolkit.Application.Abstractions;

/// <summary>
/// Stores/retrieves the raw bytes of a file uploaded through the document
/// editor's "attach file" toolbar button. The concrete implementation
/// lives in Persistence, not Infrastructure — same reasoning as
/// ITemplateLibrarySource: any implementation needs a reference to this
/// Application-layer interface, and Infrastructure is walled off from
/// Application categorically.
///
/// Deliberately no DeleteAsync: nothing currently deletes a
/// DOCUMENT_ATTACHMENT row either — see that entity's own doc comment for
/// why (an old revision may still reference it, forever). Adding deletion
/// to just this interface without a matching command to actually call it
/// from would be dead surface area.
/// </summary>
public interface IAttachmentStorage
{
    /// <summary>
    /// Persists content under a key scoped to projectId/attachmentId — the
    /// caller supplies attachmentId (the entity's own Id, generated before
    /// this call) so the storage key and the database row it belongs to
    /// are always in lockstep, with no separate id-generation step to get
    /// out of sync. Returns the StorageKey to persist on the
    /// DocumentAttachment entity; opaque to every caller except this
    /// interface's own implementation.
    /// </summary>
    Task<string> SaveAsync(
        Guid projectId, Guid attachmentId, string fileName, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored content for reading, by the StorageKey a prior
    /// SaveAsync call returned. Throws FileNotFoundException if the key
    /// doesn't resolve to an existing file — a DOCUMENT_ATTACHMENT row
    /// with no corresponding bytes on disk is a corrupt-deployment state
    /// (e.g. the storage volume was reset without also clearing the
    /// database), not a normal "not found" the caller should have to
    /// specifically anticipate.
    /// </summary>
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
}
