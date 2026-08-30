using Microsoft.EntityFrameworkCore;

namespace ArchitectureToolkit.Persistence.Providers;

public sealed class PostgreSqlDatabaseProvider : IDatabaseProvider
{
    public void ConfigureEfCore(DbContextOptionsBuilder optionsBuilder, string connectionString)
    {
        optionsBuilder.UseNpgsql(connectionString);
    }
}
