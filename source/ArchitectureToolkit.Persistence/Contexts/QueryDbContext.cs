
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace ArchitectureToolkit.Persistence.Contexts;

public sealed class QueryDbContext(DbContextOptions<QueryDbContext> options)
    : BaseDbContext<QueryDbContext>(options), IQueryDbContext
{
    IQueryable<TEntity> IQueryDbContext.Set<TEntity>()
    {
        return base.Set<TEntity>();
    }

    public Task<List<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
    where TEntity : Entity
    {
        return query.ToListAsync(cancellationToken);
    }

    public Task<TEntity?> SingleOrDefaultAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        return query.SingleOrDefaultAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        base.OnConfiguring(optionsBuilder);
    }
}
