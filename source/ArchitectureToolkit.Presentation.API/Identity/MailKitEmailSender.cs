using ArchitectureToolkit.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ArchitectureToolkit.Presentation.API.Identity;

/// <summary>
/// IEmailSender implemented via MailKit (SMTP Client Library Trade Study,
/// ADR-0018) — lives here rather than in Infrastructure because
/// Infrastructure is walled off from Application by an enforced fitness
/// test (InfrastructureArchitectureTests) and so cannot implement an
/// Application interface at all; Presentation.API is the only project
/// that references both Application (for IEmailSender) and a mail
/// library. Only ever called when SmtpConfiguration.IsConfigured is true
/// — IdentityAccountService checks that before constructing this call.
/// </summary>
public sealed class MailKitEmailSender(IOptions<SmtpConfiguration> smtpOptions) : IEmailSender
{
    public async Task SendAsync(
        string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var config = smtpOptions.Value;

        if (string.IsNullOrWhiteSpace(config.Host))
        {
            // Should never happen — IdentityAccountService only calls
            // IEmailSender when SmtpConfiguration.IsConfigured is true.
            // Guarding explicitly rather than trusting that invariant
            // silently, and it satisfies nullable analysis on host below.
            throw new InvalidOperationException(
                "MailKitEmailSender.SendAsync was called without Smtp:Host configured.");
        }
        var host = config.Host;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        var secureSocketOptions = config.UseSslOnConnect
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        await client.ConnectAsync(host, config.Port, secureSocketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(config.Username))
        {
            await client.AuthenticateAsync(config.Username, config.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
