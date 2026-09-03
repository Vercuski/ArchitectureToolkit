namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

/// <summary>
/// Everything the first-run Setup Wizard's single Save button submits in
/// one call — the connection/template/authentication/SMTP configuration
/// screen and the initial-user screen together (see SetupController).
/// </summary>
public sealed record CompleteSetupRequest
{
    public required string QueryDbConnection { get; init; }

    public required string CommandDbConnection { get; init; }

    public required string TemplateLibraryRootPath { get; init; }

    /// <summary>Left blank to use the self-hosted OpenIddict default.</summary>
    public string? Authority { get; init; }

    public required string ClientId { get; init; }

    public required string Audience { get; init; }

    /// <summary>Left blank to disable outbound email entirely (ADR-0018).</summary>
    public string? SmtpHost { get; init; }

    public required int SmtpPort { get; init; }

    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    public required string SmtpFromAddress { get; init; }

    public required string SmtpFromName { get; init; }

    public required bool SmtpUseSslOnConnect { get; init; }

    public required string InitialUserEmail { get; init; }

    public required string InitialUserPassword { get; init; }

    public required string InitialUserConfirmPassword { get; init; }
}
