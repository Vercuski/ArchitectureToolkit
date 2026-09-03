namespace ArchitectureToolkit.Infrastructure.Setup;

/// <summary>
/// The complete set of secrets/configuration this self-hosted deployment
/// needs to run, encrypted at rest by <see cref="ProtectedFileAppConfigurationStore"/>
/// and projected into <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
/// at startup by <see cref="ProtectedFileConfigurationProvider"/>. Replaces the
/// ConnectionStrings/Authentication/Smtp/TemplateLibrary sections that used to live
/// in appsettings.json/environment variables (see the "Removing appsettings.json
/// secrets" ADR).
///
/// Deliberately a plain sealed record with zero dependency on Domain, Application,
/// or Persistence — Infrastructure's zero-project-reference invariant
/// (InfrastructureArchitectureTests.InfrastructureAssembly_ShouldNot_Reference...)
/// applies here exactly as it does everywhere else in this namespace.
/// </summary>
public sealed record PersistedAppConfiguration
{
    public required string QueryDbConnection { get; init; }

    public required string CommandDbConnection { get; init; }

    public required string TemplateLibraryRootPath { get; init; }

    /// <summary>
    /// Left null/blank for the zero-config self-hosted OpenIddict default
    /// (AuthenticationConfiguration.UseSelfHostedProvider) — deliberately
    /// the only field in this record with no "required" counterpart in
    /// CompleteSetupRequest, matching that existing convention exactly.
    /// </summary>
    public string? Authority { get; init; }

    public required string ClientId { get; init; }

    public required string Audience { get; init; }

    public string? SmtpHost { get; init; }

    public required int SmtpPort { get; init; }

    public string? SmtpUsername { get; init; }

    public string? SmtpPassword { get; init; }

    public required string SmtpFromAddress { get; init; }

    public required string SmtpFromName { get; init; }

    public required bool SmtpUseSslOnConnect { get; init; }

    /// <summary>
    /// Protects the persisted OpenIddict signing/encryption certificates
    /// (PersistedCertificateProvisioner). Auto-generated once by
    /// SetupCompletionService rather than asked of the operator — it is a
    /// purely internal secret with no meaningful "value" for a human to
    /// choose, and generating it ourselves is one fewer manual secret to
    /// track (it previously had to be set as AUTH_KEYS_PASSWORD in .env).
    /// </summary>
    public required string AuthKeysPassword { get; init; }

    /// <summary>
    /// Set once at setup completion; consumed and cleared the next time
    /// the app boots with a real, fully-wired DI container available (see
    /// Program.cs). Deliberately holds a pre-computed ASP.NET Core
    /// Identity password hash, never the plaintext password —
    /// SetupCompletionService hashes it with a standalone
    /// PasswordHasher&lt;IdentityUser&gt; before this record is ever
    /// serialized to disk, encrypted or not.
    /// </summary>
    public PendingInitialUser? PendingInitialUser { get; init; }
}

/// <summary>
/// The first Identity login, captured during Setup Mode (before
/// Identity/OpenIddict are registered in that process at all) and
/// actually created on the next boot's real UserManager — see
/// SetupCompletionService's doc comment for why this can't happen
/// synchronously within the setup request itself.
/// </summary>
public sealed record PendingInitialUser
{
    public required string Email { get; init; }

    public required string PasswordHash { get; init; }
}
