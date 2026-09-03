using ArchitectureToolkit.Infrastructure.Setup;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using Microsoft.AspNetCore.Identity;
using Npgsql;
using System.Security.Cryptography;

namespace ArchitectureToolkit.Presentation.API.Setup;

public sealed record SetupCompletionError(string Field, string Message);

public sealed class SetupCompletionResult
{
    private SetupCompletionResult(bool succeeded, IReadOnlyList<SetupCompletionError> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public IReadOnlyList<SetupCompletionError> Errors { get; }

    public static SetupCompletionResult Success() => new(true, []);

    public static SetupCompletionResult Failure(IReadOnlyList<SetupCompletionError> errors) => new(false, errors);
}

/// <summary>
/// Everything that happens when the first-run Setup Wizard's Save button
/// is pressed (see the "Removing appsettings.json secrets" ADR).
///
/// Lives in Presentation.API rather than Infrastructure or
/// Persistence — same reasoning as IIdentityAccountService/
/// MailKitEmailSender's placement: it needs both a real Npgsql connection
/// (to test-connect the submitted connection strings) and
/// IAppConfigurationStore (Infrastructure), and
/// InfrastructureArchitectureTests forbids Infrastructure from seeing
/// anything else in this solution at all, so the two can only meet here.
///
/// Deliberately outside the normal MediatR/CQRS pipeline too:
/// ApplicationArchitectureTests.ApplicationAssembly_ShouldNot_ReferenceEntityFrameworkCore
/// forbids Application from referencing EF Core/Npgsql, which this needs
/// for the connection test. Same reasoning IdentityBootstrapper.SeedAsync
/// is already called directly from Program.cs rather than through a
/// command handler.
///
/// Does NOT itself run EF Core migrations, seed OpenIddict, or create the
/// initial Identity login — none of Identity/OpenIddict/the domain
/// DbContexts are registered in this (Setup Mode) process at all (see
/// Program.cs's isConfigured branch). Standing up a second, throwaway
/// copy of that whole registration here would duplicate — and risk
/// drifting from — AddIdentityAuthenticationRegistration's real one.
/// Instead, this only validates the submitted values, proves the two
/// connection strings actually work, and persists everything (including
/// a pre-hashed hold of the initial user's password —
/// see <see cref="PendingInitialUser"/>) for the *next* boot to act on
/// with the real, fully-wired DI container. See Program.cs's
/// post-migration "PendingInitialUser" step.
/// </summary>
public sealed class SetupCompletionService(
    IAppConfigurationStore configurationStore,
    IHostApplicationLifetime applicationLifetime,
    ILogger<SetupCompletionService> logger)
{
    /// <summary>
    /// How long after a successful save before the process actually
    /// stops. ASP.NET Core finishes flushing the in-flight 200 OK
    /// response regardless (StopApplication only signals a graceful
    /// shutdown, it doesn't sever open connections outright), but this
    /// margin protects against slow client networks/proxies between this
    /// process and the browser — without it, a sluggish connection could
    /// see the process exit before its response ever arrives, and the SPA
    /// would show a dropped connection instead of confirmed success.
    /// </summary>
    private static readonly TimeSpan RestartDelay = TimeSpan.FromSeconds(2);

    public async Task<SetupCompletionResult> CompleteAsync(
        CompleteSetupRequest request, CancellationToken cancellationToken)
    {
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return SetupCompletionResult.Failure(validationErrors);
        }

        var connectionErrors = await TestConnectionsAsync(request, cancellationToken);
        if (connectionErrors.Count > 0)
        {
            return SetupCompletionResult.Failure(connectionErrors);
        }

        if (!Directory.Exists(request.TemplateLibraryRootPath))
        {
            return SetupCompletionResult.Failure([
                new SetupCompletionError(
                    nameof(request.TemplateLibraryRootPath),
                    $"Template library root path '{request.TemplateLibraryRootPath}' does not exist.")
            ]);
        }

        // Hashed here, with a throwaway, standalone PasswordHasher — not
        // via UserManager<IdentityUser>.CreateAsync(user, password) —
        // because no Identity store is registered in this process at all
        // (Setup Mode). Only the hash is ever persisted; see
        // PendingInitialUser's own doc comment for where it's actually
        // turned into a login.
        var passwordHasher = new PasswordHasher<IdentityUser>();
        var placeholderUser = new IdentityUser
        {
            UserName = request.InitialUserEmail,
            Email = request.InitialUserEmail,
        };
        var passwordHash = passwordHasher.HashPassword(placeholderUser, request.InitialUserPassword);

        var configuration = new PersistedAppConfiguration
        {
            QueryDbConnection = request.QueryDbConnection,
            CommandDbConnection = request.CommandDbConnection,
            TemplateLibraryRootPath = request.TemplateLibraryRootPath,
            Authority = string.IsNullOrWhiteSpace(request.Authority) ? null : request.Authority,
            ClientId = request.ClientId,
            Audience = request.Audience,
            SmtpHost = string.IsNullOrWhiteSpace(request.SmtpHost) ? null : request.SmtpHost,
            SmtpPort = request.SmtpPort,
            SmtpUsername = string.IsNullOrWhiteSpace(request.SmtpUsername) ? null : request.SmtpUsername,
            SmtpPassword = request.SmtpPassword,
            SmtpFromAddress = request.SmtpFromAddress,
            SmtpFromName = request.SmtpFromName,
            SmtpUseSslOnConnect = request.SmtpUseSslOnConnect,
            AuthKeysPassword = GenerateAuthKeysPassword(),
            PendingInitialUser = new PendingInitialUser
            {
                Email = request.InitialUserEmail,
                PasswordHash = passwordHash,
            },
        };

        configurationStore.Save(configuration);

        logger.LogInformation(
            "Setup completed; restarting in {DelaySeconds}s so the new configuration takes effect on a fresh boot.",
            RestartDelay.TotalSeconds);

        // Fire-and-forget by design: the HTTP response for *this* request
        // must still complete normally (returning SetupCompletionResult.Success()
        // below), so the restart itself can't be awaited inline here.
        _ = ScheduleRestartAsync();

        return SetupCompletionResult.Success();
    }

    private async Task ScheduleRestartAsync()
    {
        await Task.Delay(RestartDelay);
        applicationLifetime.StopApplication();
    }

    private static List<SetupCompletionError> Validate(CompleteSetupRequest request)
    {
        var errors = new List<SetupCompletionError>();

        void Require(string? value, string field)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new SetupCompletionError(field, $"{field} is required."));
            }
        }

        Require(request.QueryDbConnection, nameof(request.QueryDbConnection));
        Require(request.CommandDbConnection, nameof(request.CommandDbConnection));
        Require(request.TemplateLibraryRootPath, nameof(request.TemplateLibraryRootPath));
        Require(request.ClientId, nameof(request.ClientId));
        Require(request.Audience, nameof(request.Audience));
        Require(request.SmtpFromAddress, nameof(request.SmtpFromAddress));
        Require(request.SmtpFromName, nameof(request.SmtpFromName));
        Require(request.InitialUserEmail, nameof(request.InitialUserEmail));
        Require(request.InitialUserPassword, nameof(request.InitialUserPassword));

        if (request.SmtpPort is <= 0 or > 65535)
        {
            errors.Add(new SetupCompletionError(nameof(request.SmtpPort), "SmtpPort must be between 1 and 65535."));
        }

        // Only checked once a password is actually present — otherwise
        // the "required" error above already covers it, and this would
        // just add a second, redundant error for the same empty field.
        if (!string.IsNullOrEmpty(request.InitialUserPassword)
            && request.InitialUserPassword != request.InitialUserConfirmPassword)
        {
            errors.Add(new SetupCompletionError(
                nameof(request.InitialUserConfirmPassword), "Password and confirmation do not match."));
        }

        return errors;
    }

    private static async Task<List<SetupCompletionError>> TestConnectionsAsync(
        CompleteSetupRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<SetupCompletionError>();

        async Task TestAsync(string connectionString, string field)
        {
            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Broad catch is deliberate here: NpgsqlException covers
                // most failures (bad host, bad credentials, unknown
                // database), but a malformed connection string throws
                // ArgumentException/FormatException before a connection
                // attempt is even made. Either way, the operator needs
                // the same thing — a clear reason this specific field is
                // wrong — not a 500 from an unhandled exception type.
                errors.Add(new SetupCompletionError(field, $"Could not connect: {ex.Message}"));
            }
        }

        await TestAsync(request.QueryDbConnection, nameof(request.QueryDbConnection));
        await TestAsync(request.CommandDbConnection, nameof(request.CommandDbConnection));

        return errors;
    }

    private static string GenerateAuthKeysPassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
