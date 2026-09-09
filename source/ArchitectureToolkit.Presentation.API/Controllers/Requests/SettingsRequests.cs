namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

public sealed record UpdateSettingsRequest
{
    public required string TemplateLibraryRootPath { get; init; }

    /// <summary>Leave blank to disable outbound email entirely, matching CompleteSetupRequest.SmtpHost.</summary>
    public string? SmtpHost { get; init; }

    public required int SmtpPort { get; init; }

    public string? SmtpUsername { get; init; }

    /// <summary>
    /// Null (omitted from the request body) leaves whatever password is
    /// currently stored unchanged — the browser is never sent the actual
    /// password to begin with, so there's nothing for the form to
    /// legitimately "leave as-is" other than by omitting this field. Any
    /// other value, including an empty string, replaces the stored
    /// password outright.
    /// </summary>
    public string? SmtpPassword { get; init; }

    public required string SmtpFromAddress { get; init; }

    public required string SmtpFromName { get; init; }

    public required bool SmtpUseSslOnConnect { get; init; }
}
