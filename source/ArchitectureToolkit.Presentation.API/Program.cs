using ArchitectureToolkit.Application;
using ArchitectureToolkit.Infrastructure;
using ArchitectureToolkit.Infrastructure.Exceptions;
using ArchitectureToolkit.Infrastructure.Identity;
using ArchitectureToolkit.Persistence;
using ArchitectureToolkit.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ADR-0003/0006 gate access by ProjectRole/SystemRole, and CreateTemplateRevision/
// CreateDocumentRevision accept BumpType — all three are enums a caller must supply
// and every response echoes back. Serializing them as their raw underlying int
// (System.Text.Json's default) makes both directions opaque to any API consumer
// who hasn't memorized the enum's declaration order. String names round-trip
// exactly as well and are self-documenting in every request/response body.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.AddApplicationRegistration();
builder.AddPersistenceRegistrations();
builder.AddInfrastructureRegistration();

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
// PostgreSQL instance just to boot the host.
if (!app.Environment.IsEnvironment("Testing"))
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
}

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseCors(DevCorsPolicy);
}

app.UseCorrelationIdMiddleware();
app.UseExceptionHandler();
app.UseIdentityAuthentication();
app.MapControllers();
app.MapRazorPages();
app.AddInfrastructureApplicationRegistration();
app.UseHttpsRedirection();

// ADR-0005: serves the Vue SPA from wwwroot (populated at publish time by
// the Dockerfile's client-build stage — see that stage's own comment).
// Placed after MapControllers/MapRazorPages so /api/* and /connect/* etc.
// are matched first; MapFallbackToFile only catches requests nothing else
// claimed, which is exactly what client-side routing needs (any deep link
// like /projects/{id} must still resolve to index.html so Vue Router can
// take over) without swallowing real API 404s.
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

await app.RunAsync();
