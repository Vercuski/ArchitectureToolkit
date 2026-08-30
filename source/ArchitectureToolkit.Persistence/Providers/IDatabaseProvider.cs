using Microsoft.EntityFrameworkCore;

namespace ArchitectureToolkit.Persistence.Providers;

public interface IDatabaseProvider
{
    void ConfigureEfCore(DbContextOptionsBuilder optionsBuilder, string connectionString);
}
