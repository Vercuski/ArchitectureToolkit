using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Application.Abstractions.Context;

public interface IQueryDbContext
{
    IQueryable<TEntity> Set<TEntity>() where TEntity : Entity;

    Task<List<TEntity>> ToListAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : Entity;

    Task<TEntity?> SingleOrDefaultAsync<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : Entity;
}
