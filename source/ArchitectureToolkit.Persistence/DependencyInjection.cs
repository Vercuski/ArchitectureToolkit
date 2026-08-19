using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.ConnectionFactory;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Abstractions.Repositories;
using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Persistence.ConnectionFactory;
using ArchitectureToolkit.Persistence.Contexts;
using ArchitectureToolkit.Persistence.Options;
using ArchitectureToolkit.Persistence.Providers;
using ArchitectureToolkit.Persistence.Repositories;

namespace ArchitectureToolkit.Persistence;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddPersistenceRegistrations(this IHostApplicationBuilder builder)
    {
        builder.AddOptionsRegistration();
        builder.AddDatabaseProviderRegistration();
        return builder;
    }

    private static IHostApplicationBuilder AddOptionsRegistration(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<ConnectionStringOptions>(GetSection<ConnectionStringOptions>(builder.Configuration));
        builder.Services.Configure<DatabasePlatformOptions>(GetSection<DatabasePlatformOptions>(builder.Configuration));
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

        builder.AddDapperPersistenceRegistrations(queryDatabaseProvider, commandDatabaseProvider);
        builder.AddEFCorePersistenceRegistrations(queryDatabaseProvider, commandDatabaseProvider);

        return builder;
    }

    private static IDatabaseProvider CreateDatabaseProvider(string platform, string side)
    {
        return platform.ToUpperInvariant() switch
        {
            "MSSQL" => new SqlServerDatabaseProvider(),
            "POSTGRESQL" => new PostgreSqlDatabaseProvider(),
            "MYSQL" => new MySQLDatabaseProvider(),
            _ => throw new NotSupportedException($"{side} Database platform '{platform}' is not supported.")
        };
    }

    private static IHostApplicationBuilder AddDapperPersistenceRegistrations(
        this IHostApplicationBuilder builder,
        IDatabaseProvider queryDatabaseProvider,
        IDatabaseProvider commandDatabaseProvider)
    {
        builder.Services.AddScoped<IDbReadOnlyConnectionFactory>(sp =>
            new DbReadOnlyConnectionFactory(
                sp.GetRequiredService<IOptions<ConnectionStringOptions>>(),
                queryDatabaseProvider));

        builder.Services.AddScoped<IDbWriteConnectionFactory>(sp =>
            new DbWriteConnectionFactory(
                sp.GetRequiredService<IOptions<ConnectionStringOptions>>(),
                commandDatabaseProvider));

        builder.Services.AddScoped<ISampleEntityDapperQueryRepository, SampleEntityDapperQueryRepository>();
        builder.Services.AddScoped<ISampleEntityDapperCommandRepository, SampleEntityDapperCommandRepository>();

        return builder;
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
