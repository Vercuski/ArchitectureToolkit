using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace ArchitectureToolkit.Persistence.Providers;

public sealed class PostgreSqlDatabaseProvider : IDatabaseProvider
{
    public void ConfigureEfCore(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }

    public IDbConnection CreateConnection(string connectionString)
    {
        return new NpgsqlConnection(connectionString);
    }
}
