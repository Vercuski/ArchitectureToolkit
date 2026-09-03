using ArchitectureToolkit.Infrastructure.Identity;
using ArchitectureToolkit.Infrastructure.Setup;
using ArchitectureToolkit.Persistence.Contexts;
using ArchitectureToolkit.Presentation.API.Controllers.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
/// MailKitEmailSender's placement: it needs both CommandDbContext/EF Core
/// migrations (Persistence) and IAppConfigurationStore/ApplicationIdentityDbContext
/// (Infrastructure), and InfrastructureArchitectureTests forbids
/// Infrastructure from seeing Persistence at all, so the two can only
/// meet here.
///
/// Deliberately outside the normal MediatR/CQRS pipeline too:
/// ApplicationArchitectureTests.ApplicationAssembly_ShouldNot_ReferenceEntityFrameworkCore
/// forbids Application from referencing EF Core, which this needs
/// directly. Same reasoning IdentityBootstrapper.SeedAsync is already
/// called directly from Program.cs rather than through a command handler.
///
/// There is deliberately no separate "test the connection" step: running
/// the actual initial migration against the submitted CommandDbConnection
/// *is* the validation — a connection that can't be reached or lacks
/// permission to create tables fails exactly the same way a throwaway
/// connect/disconnect would have, except this also leaves real, useful
/// progress in place rather than being redone from scratch after the
/// restart below. CommandDbContext and ApplicationIdentityDbContext are
/// constructed directly (not resolved from DI) because neither is
/// registered in this Setup Mode process at all (see Program.cs's
/// isConfigured branch) — deliberately: standing up a second, throwaway
/// copy of that whole registration here would duplicate, and risk
/// drifting from, AddPersistenceRegistrations'/
/// AddIdentityAuthenticationRegistration's real ones. The DbContextOptions
/// built below mirror those two registrations' EF Core configuration
/// exactly (UseNpgsql, and — for the identity context — the
/// "__EFMigrationsHistory_Identity" table name and UseOpenIddict()) for
/// that reason: this has to behave identically to the real thing, just
/// assembled by hand instead of through the DI container.
///
/// Does NOT seed OpenIddict's SPA client/scopes or create the initial
/// Identity login itself — both need services (IOpenIddictScopeManager/
/// UserManager&lt;IdentityUser&gt;) that come from the fuller Identity/
/// OpenIddict DI registration this process deliberately doesn't stand up
/// a second copy of, for the same duplication-risk reason as above.
/// Instead, this persists everything the *next* boot needs to finish the
/// job with the real, fully-wired DI container — including a pre-hashed
/// hold of the initial user's password, see
/// <see cref="PendingInitialUser"/> — and Program.cs's post-migration
/// "PendingInitialUser" step picks up from there. Re-running migrations
/// again at that next boot (ADR-0015) is safe and expected: MigrateAsync
/// only applies whatever's still pending, so having already run them here
/// makes that second call a no-op rather than a conflict.
/// </summary>
public sealed class SetupCompletionService(
    IAppConfigurationStore configurationStore,
    IHostApplicationLifetime applicationLifetime,
    IHostEnvironment environment,
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

        if (!Directory.Exists(request.TemplateLibraryRootPath))
        {
            return SetupCompletionResult.Failure([
                new SetupCompletionError(
                    nameof(request.TemplateLibraryRootPath),
                    $"Template library root path '{request.TemplateLibraryRootPath}' does not exist.")
            ]);
        }

        var migrationErrors = await RunInitialMigrationsAsync(request, cancellationToken);
        if (migrationErrors.Count > 0)
        {
            return SetupCompletionResult.Failure(migrationErrors);
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

    /// <summary>
    /// Applies CommandDbContext's and ApplicationIdentityDbContext's
    /// migrations directly against the submitted CommandDbConnection —
    /// the "initial" migrations referenced in this type's own doc
    /// comment. QueryDbConnection isn't touched here: per ADR-0012,
    /// QueryDbContext shares CommandDbContext's schema rather than owning
    /// migrations of its own, exactly as Program.cs's own post-restart
    /// migration step only ever calls MigrateAsync on these same two
    /// contexts.
    /// </summary>
    private async Task<List<SetupCompletionError>> RunInitialMigrationsAsync(
        CompleteSetupRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<SetupCompletionError>();
        var isProduction = environment.IsProduction();

        try
        {
            var commandOptionsBuilder = new DbContextOptionsBuilder<CommandDbContext>();
            commandOptionsBuilder.UseNpgsql(request.CommandDbConnection);
            if (!isProduction)
            {
                commandOptionsBuilder.EnableDetailedErrors().EnableSensitiveDataLogging();
            }

            await using var commandDbContext = new CommandDbContext(commandOptionsBuilder.Options);
            await commandDbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Broad catch is deliberate here: this covers everything from
            // an unreachable host to a malformed connection string to a
            // user without CREATE TABLE permission — the operator needs
            // the same thing regardless, a clear reason CommandDbConnection
            // specifically didn't work, not a 500 from an unhandled
            // exception type reaching the client.
            errors.Add(new SetupCompletionError(
                nameof(request.CommandDbConnection), $"Could not run initial migrations: {ex.Message}"));
            return errors;
        }

        try
        {
            var identityOptionsBuilder = new DbContextOptionsBuilder<ApplicationIdentityDbContext>();
            identityOptionsBuilder.UseNpgsql(request.CommandDbConnection, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Identity"));
            identityOptionsBuilder.UseOpenIddict();
            if (!isProduction)
            {
                identityOptionsBuilder.EnableDetailedErrors().EnableSensitiveDataLogging();
            }

            await using var identityDbContext = new ApplicationIdentityDbContext(identityOptionsBuilder.Options);
            await identityDbContext.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            errors.Add(new SetupCompletionError(
                nameof(request.CommandDbConnection), $"Could not run initial identity migrations: {ex.Message}"));
        }

        return errors;
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

    private static string GenerateAuthKeysPassword() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
