using ArchitectureToolkit.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ArchitectureToolkit.Presentation.API.HealthChecks;

/// <summary>
/// Replaces the old ArchitectureToolkit.Infrastructure.HealthChecks.SimpleHealthCheck
/// — that one returned Healthy/Degraded/Unhealthy off a Random.Next(1, 4)
/// roll and checked nothing real, which is why /health was never wired
/// into a Docker healthcheck: doing so would have caused the api
/// container to flap in and out of "unhealthy" roughly every other
/// interval for no actual reason.
///
/// This checks the one dependency that actually determines whether the
/// app can serve a request: can <see cref="CommandDbContext"/> reach
/// PostgreSQL. It deliberately does not also probe
/// ApplicationIdentityDbContext — per ADR-0012, both contexts share the
/// same physical database and connection string, so a second round trip
/// would test the same reachability twice.
///
/// Lives here, in Presentation.API, rather than alongside
/// HealthCheckConfiguration in ArchitectureToolkit.Infrastructure:
/// CommandDbContext is a Persistence type, and Infrastructure.csproj has
/// zero project references to Persistence by design (see that .csproj's
/// own comment) — Infrastructure's reflection-based health check
/// discovery (DependencyInjection.AddHealthChecksRegistration) only ever
/// scans its own assembly, so a check with a Persistence dependency
/// could never live there anyway. Presentation.API is this project's
/// established composition root for exactly this kind of cross-layer
/// wiring — the same reasoning already applied to IIdentityAccountService.
///
/// Registered directly in Program.cs (not via reflection) only inside
/// the isConfigured branch: CommandDbContext isn't in the DI container
/// at all during Setup Mode, so rather than have this type detect that
/// case itself, it's simply never constructed there.
/// </summary>
public sealed class DatabaseHealthCheck(CommandDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL connection succeeded.")
                : HealthCheckResult.Unhealthy("PostgreSQL did not respond to a connection attempt.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL connection attempt threw.", ex);
        }
    }
}
