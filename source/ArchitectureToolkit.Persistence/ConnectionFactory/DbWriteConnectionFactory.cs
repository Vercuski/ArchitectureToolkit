using Microsoft.Extensions.Options;
using ArchitectureToolkit.Application.Abstractions.ConnectionFactory;
using ArchitectureToolkit.Persistence.Options;
using ArchitectureToolkit.Persistence.Providers;
using System.Data;

namespace ArchitectureToolkit.Persistence.ConnectionFactory;

public sealed class DbWriteConnectionFactory(
    IOptions<ConnectionStringOptions> connectionStringOptions,
    IDatabaseProvider databaseProvider) : IDbWriteConnectionFactory
{
    private readonly string _connectionString = connectionStringOptions.Value.CommandDbConnection;

    public IDbConnection CreateConnection()
    {
        return databaseProvider.CreateConnection(_connectionString);
    }
}
