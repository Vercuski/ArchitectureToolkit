namespace ArchitectureToolkit.Application.Abstractions;

/// <summary>
/// Outcome of an invite (ADR-0018). EmailSent is true when IEmailSender
/// actually delivered the message; InviteLink is populated only when it
/// didn't (unconfigured SMTP or a send failure — both fall back to
/// surfacing the link rather than blocking user creation), so a caller
/// never has both a "sent" confirmation and a raw, still-live
/// password-set token sitting in a response for no reason.
/// </summary>
public sealed record UserInviteOutcome(bool EmailSent, string? InviteLink);

/// <summary>
/// Creates the ASP.NET Core Identity login (self-hosted only, ADR-0018)
/// behind a newly admin-provisioned user and gets them a way to set their
/// own password. Application code depends only on this interface — the
/// concrete implementation lives in Presentation.API, not Infrastructure
/// or Persistence: it needs UserManager&lt;IdentityUser&gt;, which lives
/// in Infrastructure, but Infrastructure is walled off from Application
/// entirely by an enforced fitness test (InfrastructureArchitectureTests)
/// and so cannot implement an Application interface at all. Presentation.API
/// is the only project that references both Application and Infrastructure,
/// which is exactly where CreateUserCommandHandler's own doc comment on
/// this same reasoning should be cross-checked against if this ever looks
/// inconsistent.
/// </summary>
public interface IIdentityAccountService
{
    /// <summary>
    /// True only when no external Authority is configured
    /// (AuthenticationConfiguration.UseSelfHostedProvider, ADR-0003) —
    /// i.e., ArchitectureToolkit itself owns password storage for this
    /// deployment. When false, InviteAsync should never be called;
    /// CreateUserCommandHandler checks this first and refuses instead.
    /// </summary>
    bool SupportsPasswordAccounts { get; }

    /// <summary>
    /// Creates a passwordless IdentityUser for <paramref name="email"/>,
    /// generates a password-reset token via
    /// UserManager.GeneratePasswordResetTokenAsync (ADR-0018 — reuses
    /// ASP.NET Core Identity's own built-in mechanism rather than a new
    /// token table), and attempts to email an invite link built from it.
    /// Deliberately does not touch the domain USER table — the caller
    /// (CreateUserCommandHandler) creates that row itself, and only after
    /// this call succeeds, so a failure here never leaves an orphaned USER
    /// row with no way to ever get credentials.
    /// </summary>
    /// <returns>
    /// Result&lt;UserInviteOutcome&gt;.Success in the normal case,
    /// regardless of whether the email itself actually sent — see
    /// UserInviteOutcome. Result.Failure only when the IdentityUser
    /// itself couldn't be created (e.g. Identity's own validation
    /// rejects it) — a genuine "this invite cannot proceed at all"
    /// case, distinct from "the account exists but the email didn't
    /// go out."
    /// </returns>
    Task<Result<UserInviteOutcome>> InviteAsync(string email, CancellationToken cancellationToken = default);
}
