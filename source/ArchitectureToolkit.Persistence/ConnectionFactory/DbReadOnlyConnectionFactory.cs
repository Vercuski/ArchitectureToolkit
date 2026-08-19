using Microsoft.Extensions.Options;
using ArchitectureToolkit.Application.Abstractions.ConnectionFactory;
using ArchitectureToolkit.Persistence.Options;
using ArchitectureToolkit.Persistence.Providers;
using System.Data;

namespace ArchitectureToolkit.Persistence.ConnectionFactory;

public sealed class DbReadOnlyConnectionFactory(
    IOptions<ConnectionStringOptions> connectionStringOptions,
    IDatabaseProvider databaseProvider) : IDbReadOnlyConnectionFactory
{
    private readonly string _connectionString = connectionStringOptions.Value.QueryDbConnection;

    public IDbConnection CreateConnection()
    {
        return databaseProvider.CreateConnection(_connectionString);
    }
}
