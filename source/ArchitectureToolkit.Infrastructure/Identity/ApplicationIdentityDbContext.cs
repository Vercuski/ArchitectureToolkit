using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArchitectureToolkit.Infrastructure.Identity;

/// <summary>
/// Persists ASP.NET Core Identity's own user/role stores and OpenIddict's
/// server stores (Applications, Authorizations, Scopes, Tokens) — the
/// self-hosted default identity provider described in ADR-0003.
///
/// Deliberately separate from ArchitectureToolkit.Persistence.Contexts'
/// CommandDbContext/QueryDbContext: this context is owned entirely by
/// Infrastructure, has zero reference to Domain, Application, or
/// Persistence, and is registered with its own migration history table
/// (see DependencyInjection.AddIdentityAuthenticationRegistration) so its
/// schema evolves independently of the domain model's, even though both
/// share the same physical PostgreSQL database (ADR-0003 v1.0.2, following
/// the ADR-0012 single-database-multiple-contexts precedent).
///
/// IdentityUser here is entirely internal to the self-hosted default
/// provider — it is never the application's own USER entity. Resolving a
/// validated token's claims to the app's own USER row is
/// IUserProvisioningService's job (Persistence), regardless of whether the
/// token came from this local provider or an external one.
/// </summary>
public sealed class ApplicationIdentityDbContext(DbContextOptions<ApplicationIdentityDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Adds OpenIddict's own entity configurations (Applications,
        // Authorizations, Scopes, Tokens) to this same context/database.
        builder.UseOpenIddict();
    }
}
