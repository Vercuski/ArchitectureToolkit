using Microsoft.EntityFrameworkCore;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Persistence.Contexts;

public abstract class BaseDbContext<T>(DbContextOptions<T> options) : DbContext(options) where T : DbContext
{
    public DbSet<SampleEntityDefinition> SampleEntity { get; set; }
}
