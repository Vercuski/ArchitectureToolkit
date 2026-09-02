using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ArchitectureToolkit.Presentation.API.Identity;

/// <summary>
/// IIdentityAccountService implementation (ADR-0018) — lives here, not
/// Infrastructure or Persistence, because it needs
/// UserManager&lt;IdentityUser&gt; (Infrastructure) and IEmailSender
/// (Application), and Infrastructure is walled off from Application by an
/// enforced fitness test (InfrastructureArchitectureTests). Presentation.API
/// is the only project that references both.
/// </summary>
public sealed class IdentityAccountService(
    UserManager<IdentityUser> userManager,
    IEmailSender emailSender,
    IOptions<AuthenticationConfiguration> authOptions,
    IOptions<SmtpConfiguration> smtpOptions)
    : IIdentityAccountService
{
    public bool SupportsPasswordAccounts => authOptions.Value.UseSelfHostedProvider;

    public async Task<Result<UserInviteOutcome>> InviteAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var identityUser = new IdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
        };

        // No password supplied — PasswordHash stays null, so this account
        // cannot sign in until ResetPasswordAsync (via the emailed/shown
        // link's token) sets one. Same CreateAsync ASP.NET Core Identity
        // always uses; omitting the password argument is what leaves it
        // unset rather than requiring one up front.
        var createResult = await userManager.CreateAsync(identityUser);
        if (!createResult.Succeeded)
        {
            var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
            return Result<UserInviteOutcome>.Failure(
                $"Could not create an identity account for this email: {errors}", ResultErrorType.Conflict);
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(identityUser);
        var inviteLink = BuildInviteLink(email, token);

        var smtpConfig = smtpOptions.Value;
        var emailSent = false;

        if (smtpConfig.IsConfigured)
        {
            try
            {
                await emailSender.SendAsync(
                    email,
                    "You've been invited to ArchitectureToolkit",
                    BuildInviteEmailHtml(inviteLink),
                    cancellationToken);
                emailSent = true;
            }
            catch
            {
                // Falls through to EmailSent = false below — a send
                // failure (bad credentials, unreachable relay, etc.)
                // degrades to the same "show the link" outcome as SMTP
                // being unconfigured in the first place, rather than
                // failing the whole command. The architect still needs
                // some way to get this person access; a misconfigured
                // mail relay shouldn't block that. See ADR-0018.
            }
        }

        return Result<UserInviteOutcome>.Success(
            new UserInviteOutcome(EmailSent: emailSent, InviteLink: emailSent ? null : inviteLink));
    }

    /// <summary>
    /// Derives the SPA's own origin from RedirectUris[0] — the one piece
    /// of configuration every self-hosted deployment must already set
    /// correctly for OIDC redirects to function at all (ADR-0018 —
    /// deliberately reuses this instead of adding a second "what's my
    /// public URL" setting that could drift out of sync with it).
    /// </summary>
    private string BuildInviteLink(string email, string token)
    {
        var redirectUri = authOptions.Value.RedirectUris.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Authentication:RedirectUris is empty — cannot build an invite link without a known SPA origin.");

        var origin = new Uri(redirectUri).GetLeftPart(UriPartial.Authority);
        var encodedEmail = Uri.EscapeDataString(email);
        var encodedToken = Uri.EscapeDataString(token);

        return $"{origin}/set-password?email={encodedEmail}&token={encodedToken}";
    }

    private static string BuildInviteEmailHtml(string inviteLink)
    {
        return $"""
            <p>You've been invited to ArchitectureToolkit.</p>
            <p><a href="{inviteLink}">Click here to set your password</a> and get started.</p>
            <p>If you weren't expecting this, you can ignore this email.</p>
            """;
    }
}
