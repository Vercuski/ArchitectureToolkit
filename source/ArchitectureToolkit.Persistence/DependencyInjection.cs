using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Persistence.AttachmentStorage;
using ArchitectureToolkit.Persistence.Contexts;
using ArchitectureToolkit.Persistence.Options;
using ArchitectureToolkit.Persistence.Providers;
using ArchitectureToolkit.Persistence.TemplateLibrary;
using ArchitectureToolkit.Persistence.UserProvisioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ArchitectureToolkit.Persistence;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddPersistenceRegistrations(this IHostApplicationBuilder builder)
    {
        builder.AddOptionsRegistration();
        builder.AddDatabaseProviderRegistration();
        builder.AddTemplateLibraryRegistration();
        builder.AddAttachmentStorageRegistration();
        builder.AddUserProvisioningRegistration();
        return builder;
    }

    private static IHostApplicationBuilder AddUserProvisioningRegistration(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        return builder;
    }

    private static IHostApplicationBuilder AddOptionsRegistration(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<ConnectionStringOptions>(GetSection<ConnectionStringOptions>(builder.Configuration));
        builder.Services.Configure<DatabasePlatformOptions>(GetSection<DatabasePlatformOptions>(builder.Configuration));
        builder.Services.Configure<TemplateLibraryOptions>(GetSection<TemplateLibraryOptions>(builder.Configuration));
        builder.Services.Configure<AttachmentStorageOptions>(GetSection<AttachmentStorageOptions>(builder.Configuration));
        return builder;
    }

    private static IHostApplicationBuilder AddTemplateLibraryRegistration(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<ITemplateLibrarySource, FileSystemTemplateLibrarySource>();
        return builder;
    }

    private static IHostApplicationBuilder AddAttachmentStorageRegistration(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IAttachmentStorage, FileSystemAttachmentStorage>();
        return builder;
    }

    private static IHostApplicationBuilder AddDatabaseProviderRegistration(
        this IHostApplicationBuilder builder)
    {
        var databasePlatformOptions = GetSection<DatabasePlatformOptions>(builder.Configuration)
            .Get<DatabasePlatformOptions>()
            ?? throw new InvalidOperationException("Missing or invalid 'DatabasePlatform' configuration section.");

        var queryDatabaseProvider = CreateDatabaseProvider(databasePlatformOptions.QueryDbPlatform, "Query");
        var commandDatabaseProvider = CreateDatabaseProvider(databasePlatformOptions.CommandDbPlatform, "Command");

        builder.AddEFCorePersistenceRegistrations(queryDatabaseProvider, commandDatabaseProvider);

        return builder;
    }

    private static IDatabaseProvider CreateDatabaseProvider(string platform, string side)
    {
        return platform.ToUpperInvariant() switch
        {
            "POSTGRESQL" => new PostgreSqlDatabaseProvider(),
            _ => throw new NotSupportedException($"{side} Database platform '{platform}' is not supported.")
        };
    }

    private static IHostApplicationBuilder AddEFCorePersistenceRegistrations(
        this IHostApplicationBuilder builder,
        IDatabaseProvider queryDatabaseProvider,
        IDatabaseProvider commandDatabaseProvider)
    {
        builder.Services.AddDbContext<CommandDbContext>((sp, options) =>
        {
            var connectionStringOptions = sp.GetRequiredService<IOptions<ConnectionStringOptions>>().Value;
            commandDatabaseProvider.ConfigureEfCore(options, connectionStringOptions.CommandDbConnection);
            if (!builder.Environment.IsProduction())
            {
                options.EnableDetailedErrors().EnableSensitiveDataLogging();
            }
        }, ServiceLifetime.Scoped);

        builder.Services.AddDbContext<QueryDbContext>((sp, options) =>
        {
            var connectionStringOptions = sp.GetRequiredService<IOptions<ConnectionStringOptions>>().Value;
            queryDatabaseProvider.ConfigureEfCore(options, connectionStringOptions.QueryDbConnection);
            if (!builder.Environment.IsProduction())
            {
                options.EnableDetailedErrors().EnableSensitiveDataLogging();
            }
        }, ServiceLifetime.Scoped);

        builder.Services.AddScoped<ICommandDbContext>(sp => sp.GetRequiredService<CommandDbContext>());
        builder.Services.AddScoped<IQueryDbContext>(sp => sp.GetRequiredService<QueryDbContext>());
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CommandDbContext>());

        return builder;
    }

    private static IConfigurationSection GetSection<T>(IConfiguration configuration)
    where T : IBaseOptionsConfig
    {
        var config = Activator.CreateInstance<T>()!;
        var section = ((IBaseOptionsConfig)config).Section;
        return configuration.GetSection(section);
    }
}
