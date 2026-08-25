using ArchitectureToolkit.Application;
using ArchitectureToolkit.Infrastructure;
using ArchitectureToolkit.Infrastructure.Exceptions;
using ArchitectureToolkit.Infrastructure.Identity;
using ArchitectureToolkit.Persistence;
using ArchitectureToolkit.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.AddApplicationRegistration();
builder.AddPersistenceRegistrations();
builder.AddInfrastructureRegistration();

builder.Services.AddEndpointsApiExplorer();

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
}

app.UseCorrelationIdMiddleware();
app.UseExceptionHandler();
app.UseIdentityAuthentication();
app.MapControllers();
app.MapRazorPages();
app.AddInfrastructureApplicationRegistration();
app.UseHttpsRedirection();
await app.RunAsync();
