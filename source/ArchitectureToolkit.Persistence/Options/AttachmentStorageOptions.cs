using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Persistence.Options;

/// <summary>
/// Where uploaded document attachments live on disk. A path, not a
/// secret, so — like TemplateLibrary:RootPath was before ADR the "Removing
/// appsettings.json secrets" work, and like Setup:StorageDirectory still
/// is today — this is deliberately plain configuration (appsettings.json/
/// environment variable), not part of the Setup Wizard's encrypted blob.
/// </summary>
public sealed record AttachmentStorageOptions : IBaseOptionsConfig
{
    /// <summary>
    /// Local dev defaults to "App_Data/attachments" under the API
    /// project's own directory (same App_Data convention Setup:StorageDirectory
    /// uses, and already covered by .gitignore's "App_Data/" entry); the
    /// container overrides this to a dedicated volume mount
    /// (see docker-compose.yml).
    /// </summary>
    public string RootPath { get; set; } = null!;

    public string Section => "Attachments";
}
