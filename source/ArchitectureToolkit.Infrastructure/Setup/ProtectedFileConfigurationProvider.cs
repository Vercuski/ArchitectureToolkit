using Microsoft.Extensions.Configuration;

namespace ArchitectureToolkit.Infrastructure.Setup;

public sealed class ProtectedFileConfigurationProvider(IAppConfigurationStore store) : ConfigurationProvider
{
    public override void Load()
    {
        // DatabasePlatform is ADR-0002-locked to PostgreSQL — not
        // something Setup asks the operator for at all, but
        // AddDatabaseProviderRegistration still reads it from
        // IConfiguration, so it's supplied here unconditionally rather
        // than inventing a second configuration mechanism for one fixed
        // value. Present regardless of whether the store is configured
        // yet, since it never depends on anything the wizard collects.
        Data["DatabasePlatform:QueryDbPlatform"] = "PostgreSQL";
        Data["DatabasePlatform:CommandDbPlatform"] = "PostgreSQL";

        var configuration = store.Load();
        if (configuration is null)
        {
            // No encrypted blob yet — contribute nothing else. Every
            // section a fresh install's Persistence/Infrastructure
            // registration needs (ConnectionStrings, Authentication,
            // TemplateLibrary, Smtp) stays entirely absent, which is
            // exactly the signal Program.cs uses to enter Setup Mode.
            return;
        }

        Data["ConnectionStrings:QueryDbConnection"] = configuration.QueryDbConnection;
        Data["ConnectionStrings:CommandDbConnection"] = configuration.CommandDbConnection;

        Data["TemplateLibrary:RootPath"] = configuration.TemplateLibraryRootPath;

        Data["Authentication:Authority"] = configuration.Authority ?? string.Empty;
        Data["Authentication:ClientId"] = configuration.ClientId;
        Data["Authentication:Audience"] = configuration.Audience;
        Data["Authentication:KeysPassword"] = configuration.AuthKeysPassword;

        Data["Smtp:Host"] = configuration.SmtpHost ?? string.Empty;
        Data["Smtp:Port"] = configuration.SmtpPort.ToString();
        Data["Smtp:Username"] = configuration.SmtpUsername ?? string.Empty;
        Data["Smtp:Password"] = configuration.SmtpPassword ?? string.Empty;
        Data["Smtp:FromAddress"] = configuration.SmtpFromAddress;
        Data["Smtp:FromName"] = configuration.SmtpFromName;
        Data["Smtp:UseSslOnConnect"] = configuration.SmtpUseSslOnConnect.ToString();
    }
}
