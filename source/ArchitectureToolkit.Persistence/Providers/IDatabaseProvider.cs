using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ArchitectureToolkit.Persistence.Providers;

public interface IDatabaseProvider
{
    void ConfigureEfCore(DbContextOptionsBuilder optionsBuilder, string connectionString);
}
