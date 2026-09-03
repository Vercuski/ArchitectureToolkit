using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Persistence.Options;
using Microsoft.Extensions.Options;

namespace ArchitectureToolkit.Persistence.AttachmentStorage;

public sealed class FileSystemAttachmentStorage(IOptions<AttachmentStorageOptions> options) : IAttachmentStorage
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(
        Guid projectId, Guid attachmentId, string fileName, Stream content, CancellationToken cancellationToken = default)
    {
        // {projectId}/{attachmentId}/{sanitized file name} — the
        // attachmentId directory is what actually guarantees uniqueness
        // (two uploads named "diagram.png" never collide); the original
        // file name is kept as the leaf purely so a download's
        // Content-Disposition can offer back the name the operator
        // recognizes, not a GUID.
        var storageKey = Path.Combine(projectId.ToString(), attachmentId.ToString(), SanitizeFileName(fileName));
        var fullPath = ResolveFullPath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var destination = File.Create(fullPath);
        await content.CopyToAsync(destination, cancellationToken);

        return storageKey;
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(storageKey);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                $"Attachment storage key '{storageKey}' does not resolve to an existing file at '{fullPath}'.",
                fullPath);
        }

        // Buffered into memory rather than returning the raw FileStream:
        // the controller action that calls this closes over the returned
        // stream for the lifetime of the HTTP response, and an unbounded
        // number of concurrently open FileStreams is a real, easy-to-hit
        // resource limit for a self-hosted deployment in a way a handful
        // of buffered MemoryStreams for files already capped at
        // MaxUploadSizeBytes (see UploadDocumentAttachmentCommandHandler)
        // is not.
        var buffer = new MemoryStream();
        await using (var source = File.OpenRead(fullPath))
        {
            await source.CopyToAsync(buffer, cancellationToken);
        }
        buffer.Position = 0;
        return buffer;
    }

    /// <summary>
    /// Strips path separators and other characters that would let a
    /// crafted file name escape its {projectId}/{attachmentId} directory
    /// (or just produce an invalid path) — the one part of storageKey
    /// that's ever derived from user input rather than server-generated
    /// Guids.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName); // drops any directory component outright
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string([.. name.Select(c => invalid.Contains(c) ? '_' : c)]).Trim();

        return sanitized.Length == 0 ? "file" : sanitized;
    }

    private string ResolveFullPath(string storageKey)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));

        // Defense in depth alongside SanitizeFileName: confirms the
        // resolved path never lands outside _rootPath regardless of how
        // storageKey was produced (a belt-and-suspenders check now that
        // storageKey has started round-tripping through the database,
        // rather than only ever being the value SaveAsync just returned
        // in the same call).
        if (!fullPath.StartsWith(_rootPath + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Storage key '{storageKey}' resolves outside the attachment storage root.");
        }

        return fullPath;
    }
}
