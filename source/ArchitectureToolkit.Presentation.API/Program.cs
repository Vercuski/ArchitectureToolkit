using ArchitectureToolkit.Application;
using ArchitectureToolkit.Infrastructure;
using ArchitectureToolkit.Infrastructure.Exceptions;
using ArchitectureToolkit.Infrastructure.Identity;
using ArchitectureToolkit.Infrastructure.Setup;
using ArchitectureToolkit.Persistence;
using ArchitectureToolkit.Persistence.Contexts;
using ArchitectureToolkit.Presentation.API.Identity;
using ArchitectureToolkit.Presentation.API.Setup;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// First-run setup (see the "Removing appsettings.json secrets" ADR):
// added before anything else touches configuration, and ahead of
// AddPersistenceRegistrations/AddInfrastructureRegistration below, so
// this deployment's ConnectionStrings/Authentication/Smtp/TemplateLibrary
// sections come from here rather than appsettings.json/environment
// variables. Setup:StorageDirectory is a path, not a secret, so it's
// fine to keep in ordinary configuration — docker-compose.yml points it
// at the app-config volume's mount point; local dev falls back to a
// folder under the content root (see .gitignore's App_Data/ entry).
var storageDirectory = builder.Configuration["Setup:StorageDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "config");
var appConfigurationStore = new ProtectedFileAppConfigurationStore(storageDirectory);
// ConfigurationManager (the concrete type behind builder.Configuration)
// implements IConfigurationBuilder.Add explicitly, so it isn't callable
// directly on it — Sources is the one IConfigurationBuilder member it
// exposes as an ordinary public property instead.
builder.Configuration.Sources.Add(new ProtectedFileConfigurationSource(appConfigurationStore));
builder.Services.AddSingleton<IAppConfigurationStore>(appConfigurationStore);

// Not something Setup asks the operator for — a fixed deployment
// convention derived from the same storage root, consolidating what used
// to be a second, separately-configured "identity-keys" volume/
// AUTH_KEYS_PASSWORD secret into this one. Set directly (not via the
// encrypted store) because it's a path, not a secret.
builder.Configuration["Authentication:KeysDirectory"] = Path.Combine(storageDirectory, "openiddict-certs");

// The single "is this deployment configured" signal (see SetupState's
// doc comment for why this checks the fully-layered IConfiguration
// rather than appConfigurationStore.IsConfigured directly): true once
// *some* configuration source — the encrypted store on a real
// deployment, or appsettings.Testing.json for WebApplicationFactory-based
// tests — actually supplies a connection string.
var isConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["ConnectionStrings:CommandDbConnection"]);
builder.Services.AddSingleton(new SetupState(isConfigured));

// ADR-0003/0006 gate access by ProjectRole/SystemRole, and CreateTemplateRevision/
// CreateDocumentRevision accept BumpType — all three are enums a caller must supply
// and every response echoes back. Serializing them as their raw underlying int
// (System.Text.Json's default) makes both directions opaque to any API consumer
// who hasn't memorized the enum's declaration order. String names round-trip
// exactly as well and are self-documenting in every request/response body.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<SetupCompletionService>();

if (isConfigured)
{
    builder.Services.AddRazorPages();
    builder.AddApplicationRegistration();
    builder.AddPersistenceRegistrations();
    builder.AddInfrastructureRegistration();
    builder.AddIdentityAccountServices();
}
else
{
    // Setup Mode: none of the registrations above can run without a real
    // connection string — AddIdentityAuthenticationRegistration throws
    // without one, and every DbContext registration needs one too — so
    // only the dependency-free slice of Infrastructure (health checks,
    // logging, correlation, ProblemDetails) comes up here. SetupController
    // (which needs none of this) is still registered below via
    // AddControllers(), and is the only controller reachable in this mode.
    builder.AddCoreInfrastructureRegistration();
}

builder.Services.AddEndpointsApiExplorer();

// Dev-only: the Vue dev server runs on its own origin (typically
// localhost:5173) during local development, unlike production, where the
// SPA is served from this API's own wwwroot and is therefore genuinely
// same-origin (ADR-0005) — CORS is never needed there, matching that
// ADR's stated consequence. This policy exists purely so oidc-client-ts
// and the future API client can reach this API directly during dev
// without a proxy; it's registered unconditionally (CORS options are
// cheap) but only ever applied via UseCors below when not Production.
const string DevCorsPolicy = "DevSpaOrigin";
builder.Services.AddCors(options => options.AddPolicy(DevCorsPolicy, policy => policy
    .WithOrigins("http://localhost:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

// ADR-0015: auto-apply migrations on startup so a fresh `docker compose up`
// needs no separate `dotnet ef database update` step. Skipped in the
// `Testing` environment (see ArchitectureToolkit.Tests' AssemblySetup) so
// WebApplicationFactory-based tests that never touch the database — e.g.
// CorrelationIdIntegrationTests hitting /health — don't require a reachable
// PostgreSQL instance just to boot the host. Also skipped whenever Setup
// Mode is active: none of CommandDbContext/ApplicationIdentityDbContext is
// even registered in that case (see the isConfigured branch above).
if (isConfigured && !app.Environment.IsEnvironment("Testing"))
{
    using var migrationScope = app.Services.CreateScope();
    var commandDbContext = migrationScope.ServiceProvider.GetRequiredService<CommandDbContext>();
    await commandDbContext.Database.MigrateAsync();

    // ADR-0003 v1.0.2: ApplicationIdentityDbContext tracks its own,
    // separate migration history (__EFMigrationsHistory_Identity), so it
    // needs its own MigrateAsync call alongside CommandDbContext's.
    var identityDbContext = migrationScope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();
    await identityDbContext.Database.MigrateAsync();

    // ADR-0003 follow-up: seeds the SPA's OAuth client and (optionally) a
    // bootstrap admin login — only meaningful for the self-hosted default;
    // an external Authority manages its own client registrations.
    var authConfig = migrationScope.ServiceProvider.GetRequiredService<IOptions<AuthenticationConfiguration>>().Value;
    if (authConfig.UseSelfHostedProvider)
    {
        await IdentityBootstrapper.SeedAsync(migrationScope.ServiceProvider, authConfig);
    }

    // First-run setup, continued (see SetupCompletionService's doc
    // comment): the Setup Wizard could only pre-hash and stash the
    // initial login's password — Identity/OpenIddict weren't registered
    // yet in that (Setup Mode) process. This is the first boot where they
    // are, so this is where that pending login actually gets created,
    // using the real, fully-wired UserManager — exactly what
    // Register.cshtml.cs does for any subsequent first-user registration.
    // Cleared immediately after so a later restart (e.g. a redeploy)
    // never tries to recreate it — HasAnyIdentityUsersAsync would already
    // be true by then regardless, but there's no reason to keep an
    // already-consumed password hash sitting in the encrypted blob.
    var pendingInitialUser = appConfigurationStore.Load()?.PendingInitialUser;
    if (pendingInitialUser is not null)
    {
        var userManager = migrationScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (!await IdentityBootstrapper.HasAnyIdentityUsersAsync(userManager))
        {
            var identityUser = new IdentityUser
            {
                UserName = pendingInitialUser.Email,
                Email = pendingInitialUser.Email,
                EmailConfirmed = true,
                PasswordHash = pendingInitialUser.PasswordHash,
            };
            await userManager.CreateAsync(identityUser);
        }

        var currentConfiguration = appConfigurationStore.Load();
        if (currentConfiguration is not null)
        {
            appConfigurationStore.Save(currentConfiguration with { PendingInitialUser = null });
        }
    }
}

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors(DevCorsPolicy);
}

app.UseCorrelationIdMiddleware();
app.UseExceptionHandler();

if (isConfigured)
{
    app.UseIdentityAuthentication();
}

app.MapControllers();

if (isConfigured)
{
    app.MapRazorPages();
}

app.AddInfrastructureApplicationRegistration();
app.UseHttpsRedirection();

// ADR-0005: serves the Vue SPA from wwwroot (populated at publish time by
// the Dockerfile's client-build stage — see that stage's own comment).
// Placed after MapControllers/MapRazorPages so /api/* and /connect/* etc.
// are matched first; MapFallbackToFile only catches requests nothing else
// claimed, which is exactly what client-side routing needs (any deep link
// like /projects/{id} must still resolve to index.html so Vue Router can
// take over) without swallowing real API 404s. The SPA itself decides
// whether to render the Setup Wizard or the normal app shell based on
// GET /api/setup/status — see router/index.ts.
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();
