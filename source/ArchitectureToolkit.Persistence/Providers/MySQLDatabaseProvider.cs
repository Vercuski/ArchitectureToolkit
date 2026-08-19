using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using System.Data;

namespace ArchitectureToolkit.Persistence.Providers;

public sealed class MySQLDatabaseProvider : IDatabaseProvider
{
    public void ConfigureEfCore(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        optionsBuilder.UseMySQL(connectionString);
    }

    public IDbConnection CreateConnection(string connectionString)
    {
        return new MySqlConnection(connectionString);
    }
}
