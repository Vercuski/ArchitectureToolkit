namespace ArchitectureToolkit.Presentation.API.Identity;

/// <summary>
/// Bound from the "Smtp" configuration section (ADR-0018). Left with an
/// empty/unset Host, ArchitectureToolkit sends no email — MailKitEmailSender
/// is never invoked, and IdentityAccountService.InviteAsync falls back to
/// surfacing the raw invite link instead. Same config-driven-default shape
/// as AuthenticationConfiguration.UseSelfHostedProvider.
/// </summary>
public sealed class SmtpConfiguration
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "no-reply@architecturetoolkit.local";
    public string FromName { get; set; } = "ArchitectureToolkit";

    /// <summary>
    /// True for a direct TLS connection (typically port 465). False (the
    /// common case) uses StartTLS when the server offers it — MailKit's
    /// SecureSocketOptions.StartTlsWhenAvailable, which fits the far more
    /// common port-587 SMTP-submission setup (Gmail app passwords,
    /// Office365, Postmark/SES SMTP relay, a local Mailpit/MailHog for
    /// dev) without a separate setting for every provider's convention.
    /// </summary>
    public bool UseSslOnConnect { get; set; }

    /// <summary>
    /// True once a Host is configured — the single switch InviteAsync
    /// checks to decide whether to attempt sending at all.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
