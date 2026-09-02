namespace ArchitectureToolkit.Application.Abstractions;

/// <summary>
/// Sends an email. The only current caller is IIdentityAccountService's
/// invite flow (ADR-0018), but this stays its own port rather than being
/// folded into that interface — it's a separately testable concern (the
/// SMTP Client Library Trade Study scored testability on its own), and
/// keeping it separate means a future feature that also needs to send
/// email (a notification, a digest) has a Port to depend on already.
///
/// The concrete implementation (MailKit, per the Trade Study) lives in
/// Presentation.API, not Infrastructure or Persistence — Infrastructure
/// is walled off from Application by an enforced fitness test
/// (InfrastructureArchitectureTests), and Persistence has no reason to
/// know about SMTP. Presentation.API is the only project that references
/// both this interface and whatever concrete mail library implements it.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends a single HTML email. Throws on failure (connection refused,
    /// authentication rejected, etc.) rather than returning a Result —
    /// there's no business-level failure mode here, only technical ones,
    /// and IIdentityAccountService's InviteAsync is what decides how to
    /// treat a thrown exception (falls back to surfacing the raw invite
    /// link rather than propagating the failure — see that interface).
    /// </summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
