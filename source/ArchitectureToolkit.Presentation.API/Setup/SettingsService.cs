using ArchitectureToolkit.Infrastructure.Setup;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;

namespace ArchitectureToolkit.Presentation.API.Setup;

public sealed record SettingsUpdateError(string Field, string Message);

public sealed class SettingsUpdateResult
{
    private SettingsUpdateResult(bool succeeded, IReadOnlyList<SettingsUpdateError> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<SettingsUpdateError> Errors { get; }

    public static SettingsUpdateResult Success() => new(true, []);

    public static SettingsUpdateResult Failure(IReadOnlyList<SettingsUpdateError> errors) => new(false, errors);
}

/// <summary>
/// What GET /api/settings returns. Deliberately excludes SmtpPassword
/// itself — SmtpPasswordConfigured (a bool) is enough for the UI to show
/// "a password is currently set" without ever round-tripping the actual
/// secret back to the browser. See SettingsController's own doc comment
/// for why this covers only SMTP and the template library path, not the
/// full set of things the Setup Wizard collects.
/// </summary>
public sealed record SettingsDto(
    string TemplateLibraryRootPath,
    string? SmtpHost,
    int SmtpPort,
    string? SmtpUsername,
    bool SmtpPasswordConfigured,
    string SmtpFromAddress,
    string SmtpFromName,
    bool SmtpUseSslOnConnect);

/// <summary>
/// Reads and updates the subset of PersistedAppConfiguration that's safe
/// to change while the app is running (ADR-0024) — SMTP and the template
/// library root path. Everything else PersistedAppConfiguration holds
/// (connection strings, Authority/ClientId/Audience, AuthKeysPassword,
/// PendingInitialUser) is untouched here and carried through unchanged on
/// every update via the `with` expression in <see cref="Update"/>.
///
/// Lives here, in Presentation.API, rather than as a MediatR command/query
/// in Application: this needs IAppConfigurationStore, a Persistence/
/// Infrastructure-adjacent concern Application has no project reference
/// to at all — the same constraint SetupCompletionService's own doc
/// comment describes for its own, adjacent needs. SettingsController
/// calls this directly instead.
///
/// Shares SetupCompletionService's restart-to-apply mechanism rather than
/// attempting a hot-reload: IOptions&lt;SmtpConfiguration&gt; is bound
/// once at startup and won't pick up a change to the underlying
/// IConfiguration without a fresh boot regardless, so a short, already-
/// tested restart cycle (the same one the Setup Wizard's own "restarting"
/// screen already covers) is simpler and more consistent than adding a
/// second, IOptionsMonitor-based configuration-reload path for this one
/// feature.
/// </summary>
public sealed class SettingsService(
    IAppConfigurationStore configurationStore,
    IHostApplicationLifetime applicationLifetime,
    ILogger<SettingsService> logger)
{
    /// <summary>Same margin as SetupCompletionService's own RestartDelay — see that type's doc comment.</summary>
    private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(2);

    public SettingsDto GetCurrent()
    {
        var configuration = LoadOrThrow();

        var smtpPasswordConfigured = !string.IsNullOrEmpty(configuration.SmtpPassword);

        return new SettingsDto(
            configuration.TemplateLibraryRootPath,
            configuration.SmtpHost,
            configuration.SmtpPort,
            configuration.SmtpUsername,
            smtpPasswordConfigured,
            configuration.SmtpFromAddress,
            configuration.SmtpFromName,
            configuration.SmtpUseSslOnConnect);
    }

    public SettingsUpdateResult Update(UpdateSettingsRequest request)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return SettingsUpdateResult.Failure(errors);
        }

        var current = LoadOrThrow();

        var updated = current with
        {
            TemplateLibraryRootPath = request.TemplateLibraryRootPath,
            SmtpHost = string.IsNullOrWhiteSpace(request.SmtpHost) ? null : request.SmtpHost,
            SmtpPort = request.SmtpPort,
            SmtpUsername = string.IsNullOrWhiteSpace(request.SmtpUsername) ? null : request.SmtpUsername,
            // null means "leave the currently-stored password unchanged" —
            // see UpdateSettingsRequest.SmtpPassword's own doc comment.
            // Any other value, including "", replaces it outright: an
            // operator clearing SmtpUsername to go passwordless doesn't
            // need SmtpPassword touched at all (MailKitEmailSender already
            // skips AuthenticateAsync whenever Username is blank).
            SmtpPassword = request.SmtpPassword ?? current.SmtpPassword,
            SmtpFromAddress = request.SmtpFromAddress,
            SmtpFromName = request.SmtpFromName,
            SmtpUseSslOnConnect = request.SmtpUseSslOnConnect,
        };

        configurationStore.Save(updated);

        logger.LogInformation(
            "Settings updated; restarting in {DelaySeconds}s so the new configuration takes effect on a fresh boot.",
            RestartDelay.TotalSeconds);

        // Fire-and-forget by design, same reasoning as SetupCompletionService's
        // identical pattern: the HTTP response for this request must still
        // complete normally before the process actually stops.
        _ = ScheduleRestartAsync();

        return SettingsUpdateResult.Success();
    }

    private PersistedAppConfiguration LoadOrThrow()
    {
        // Not reachable through the API in practice: SettingsService is
        // only ever registered inside Program.cs's isConfigured branch
        // (see DependencyInjection there), and IAppConfigurationStore.Load
        // only returns null before setup has ever completed. A defensive
        // guard against a corrupt/inconsistent deployment, not an expected
        // path.
        return configurationStore.Load()
            ?? throw new InvalidOperationException(
                "SettingsService was called before initial setup has completed.");
    }

    private async Task ScheduleRestartAsync()
    {
        await Task.Delay(RestartDelay);
        applicationLifetime.StopApplication();
    }

    private static List<SettingsUpdateError> Validate(UpdateSettingsRequest request)
    {
        var errors = new List<SettingsUpdateError>();

        if (string.IsNullOrWhiteSpace(request.TemplateLibraryRootPath))
        {
            errors.Add(new SettingsUpdateError(
                nameof(request.TemplateLibraryRootPath), "Template library root path is required."));
        }
        else if (!Directory.Exists(request.TemplateLibraryRootPath))
        {
            errors.Add(new SettingsUpdateError(
                nameof(request.TemplateLibraryRootPath),
                $"Template library root path '{request.TemplateLibraryRootPath}' does not exist."));
        }

        if (string.IsNullOrWhiteSpace(request.SmtpFromAddress))
        {
            errors.Add(new SettingsUpdateError(nameof(request.SmtpFromAddress), "SMTP from address is required."));
        }

        if (string.IsNullOrWhiteSpace(request.SmtpFromName))
        {
            errors.Add(new SettingsUpdateError(nameof(request.SmtpFromName), "SMTP from name is required."));
        }

        if (request.SmtpPort is <= 0 or > 65535)
        {
            errors.Add(new SettingsUpdateError(nameof(request.SmtpPort), "SMTP port must be between 1 and 65535."));
        }

        return errors;
    }
}
