using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Application.Abstractions.Context;

public interface IQueryDbContext
{
    IQueryable<TEntity> Set<TEntity>() where TEntity : class, IPersistable;

    Task<List<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : class, IPersistable;

    Task<TEntity?> SingleOrDefaultAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : class, IPersistable;
}
