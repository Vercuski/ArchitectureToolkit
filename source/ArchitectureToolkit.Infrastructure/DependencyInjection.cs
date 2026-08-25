using ArchitectureToolkit.Infrastructure.Correlation;
using ArchitectureToolkit.Infrastructure.HealthChecks;
using ArchitectureToolkit.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenIddict.Validation.AspNetCore;
using System.Reflection;

namespace ArchitectureToolkit.Infrastructure;

public static class DependencyInjection
{
    public static WebApplication? AddInfrastructureApplicationRegistration(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckConfiguration.WriteResponse
        });
        return app;
    }

    public static WebApplication UseCorrelationIdMiddleware(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        return app;
    }

    /// <summary>
    /// Must run after routing is established (implicit in the minimal
    /// hosting model once <c>MapControllers</c> is called) and before
    /// endpoints are mapped, so <c>[Authorize]</c> and OpenIddict's own
    /// passthrough-mapped endpoints (connect/authorize, connect/token,
    /// connect/userinfo) both resolve correctly.
    /// </summary>
    public static WebApplication UseIdentityAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static IHostApplicationBuilder AddInfrastructureRegistration(this IHostApplicationBuilder builder)
    {
        builder.AddHealthChecksRegistration();
        builder.AddLoggingRegistration();
        builder.AddIdentityAuthenticationRegistration();
        builder.Services.AddSingleton<CorrelationIdAccessor>();
        builder.Services.AddProblemDetails();
        return builder;
    }

    /// <summary>
    /// ADR-0003: registers the self-hosted default identity provider
    /// (OpenIddict backed by ASP.NET Core Identity) plus provider-agnostic
    /// token validation. Entirely self-contained within Infrastructure —
    /// <see cref="ApplicationIdentityDbContext"/> is the only DbContext
    /// referenced here, never <c>ICommandDbContext</c>/<c>IUnitOfWork</c>
    /// from Persistence.
    ///
    /// Validation always goes through OpenIddict.Validation
    /// (<see cref="OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme"/>),
    /// whether the token was issued locally or by an external provider —
    /// this is what makes the "swap via config, no code change" promise in
    /// ADR-0003 hold in practice, so no separate JwtBearer registration is
    /// needed alongside it.
    /// </summary>
    private static IHostApplicationBuilder AddIdentityAuthenticationRegistration(
        this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<AuthenticationConfiguration>(
            builder.Configuration.GetSection(AuthenticationConfiguration.SectionName));

        var authConfig = builder.Configuration
            .GetSection(AuthenticationConfiguration.SectionName)
            .Get<AuthenticationConfiguration>() ?? new AuthenticationConfiguration();

        var useSelfHostedProvider = authConfig.UseSelfHostedProvider;

        var identityConnectionString = builder.Configuration["ConnectionStrings:CommandDbConnection"]
            ?? throw new InvalidOperationException(
                "Missing 'ConnectionStrings:CommandDbConnection' configuration. The self-hosted " +
                "identity provider's tables (ApplicationIdentityDbContext) share the same " +
                "physical database as the domain model, per ADR-0003.");

        builder.Services.AddDbContext<ApplicationIdentityDbContext>(options =>
        {
            options.UseNpgsql(identityConnectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory_Identity"));
            options.UseOpenIddict();
            if (!builder.Environment.IsProduction())
            {
                options.EnableDetailedErrors().EnableSensitiveDataLogging();
            }
        });

        builder.Services
            .AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationIdentityDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthorization();

        var openIddictBuilder = builder.Services.AddOpenIddict()
            .AddCore(options => options
                .UseEntityFrameworkCore()
                .UseDbContext<ApplicationIdentityDbContext>());

        openIddictBuilder.AddValidation(options =>
        {
            if (useSelfHostedProvider)
            {
                options.UseLocalServer();
            }
            else
            {
                options.SetIssuer(new Uri(authConfig.Authority!));
                options.AddAudiences(authConfig.Audience);
                options.UseSystemNetHttp();
            }

            options.UseAspNetCore();
        });

        if (useSelfHostedProvider)
        {
            openIddictBuilder.AddServer(options =>
            {
                options
                    .SetAuthorizationEndpointUris("connect/authorize")
                    .SetTokenEndpointUris("connect/token")
                    .SetUserInfoEndpointUris("connect/userinfo");

                options
                    .AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow();

                options.RegisterScopes(authConfig.Audience);

                // Development: brand-new ephemeral signing/encryption keys
                // every process start — fine locally, never for Production
                // (every previously-issued token, including refresh tokens,
                // would be invalidated on each restart/redeploy).
                //
                // Production: persisted, self-signed certificates loaded
                // from (or generated once into) a mounted volume, so they
                // survive restarts. See PersistedCertificateProvisioner and
                // ADR-0003's follow-up notes.
                if (builder.Environment.IsProduction())
                {
                    var keysDirectory = builder.Configuration["Authentication:KeysDirectory"]
                        ?? "/app/keys";
                    var keysPassword = builder.Configuration["Authentication:KeysPassword"]
                        ?? throw new InvalidOperationException(
                            "Missing 'Authentication:KeysPassword' configuration. Production " +
                            "requires a password to protect the persisted OpenIddict " +
                            "signing/encryption certificates (ADR-0003 follow-up) — " +
                            "AddDevelopmentSigningCertificate()/AddDevelopmentEncryptionCertificate() " +
                            "are development-only and are never used outside Development here.");

                    var signingCertificate = PersistedCertificateProvisioner.GetOrCreate(
                        Path.Combine(keysDirectory, "openiddict-signing.pfx"),
                        keysPassword,
                        "ArchitectureToolkit Signing");
                    var encryptionCertificate = PersistedCertificateProvisioner.GetOrCreate(
                        Path.Combine(keysDirectory, "openiddict-encryption.pfx"),
                        keysPassword,
                        "ArchitectureToolkit Encryption");

                    options
                        .AddSigningCertificate(signingCertificate)
                        .AddEncryptionCertificate(encryptionCertificate);
                }
                else
                {
                    options
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }

                options
                    .UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough();

                // Dev-only: lets the server answer over plain HTTP for local
                // testing without a TLS cert. Production always terminates
                // TLS per ADR-0010/ADR-0011, so this must never apply there.
                if (!builder.Environment.IsProduction())
                {
                    options.UseAspNetCore().DisableTransportSecurityRequirement();
                }
            });
        }

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        return builder;
    }

    private static IHostApplicationBuilder AddHealthChecksRegistration(this IHostApplicationBuilder builder)
    {
        var healthCheckBuilder = builder.Services.AddHealthChecks();
        foreach (var healthCheckType in Assembly.GetExecutingAssembly()
            .GetTypes().Where(type => !type.IsAbstract &&
            type.GetInterfaces().Contains(typeof(IHealthCheck))))
        {
            healthCheckBuilder.Add(new HealthCheckRegistration(
                healthCheckType.Name,
                serviceProvider => (IHealthCheck)ActivatorUtilities.CreateInstance(serviceProvider, healthCheckType),
                failureStatus: null,
                tags: null));
        }
        return builder;
    }

    private static IHostApplicationBuilder AddLoggingRegistration(this IHostApplicationBuilder builder)
    {
        builder.Services.AddLogging(config =>
        {
            config.ClearProviders();
            if (!builder.Environment.IsProduction())
            {
                config.AddSimpleConsole(options => options.IncludeScopes = true);
            }
        });
        return builder;
    }
}
