using ArchitectureToolkit.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ArchitectureToolkit.Presentation.API.Identity;

/// <summary>
/// Wires IIdentityAccountService/IEmailSender to their Presentation.API
/// implementations (ADR-0018). Registered from Program.cs alongside
/// AddPersistenceRegistrations()/AddInfrastructureRegistration() —
/// deliberately its own small extension here rather than folded into
/// either of those, since neither Persistence nor Infrastructure can
/// reference the other's dependencies these adapters need (see
/// IIdentityAccountService's own doc comment).
/// </summary>
public static class IdentityAccountServiceRegistration
{
    public static IHostApplicationBuilder AddIdentityAccountServices(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<SmtpConfiguration>(
            builder.Configuration.GetSection(SmtpConfiguration.SectionName));

        builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();
        builder.Services.AddScoped<IIdentityAccountService, IdentityAccountService>();

        return builder;
    }
}
