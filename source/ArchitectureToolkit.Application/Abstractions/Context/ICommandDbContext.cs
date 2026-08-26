using ArchitectureToolkit.Domain.Abstractions;
using System.Data;

namespace ArchitectureToolkit.Application.Abstractions.Context;

public interface ICommandDbContext
{
    void Insert<TEntity>(TEntity entity) where TEntity : class, IPersistable;
    void InsertRange<TEntity>(IReadOnlyCollection<TEntity> entities) where TEntity : class, IPersistable;
    void Alter<TEntity>(TEntity entity) where TEntity : class, IPersistable;
    void Delete<TEntity>(TEntity entity) where TEntity : class, IPersistable;
    int SaveChanges();
    Task<int> ExecuteSqlAsync(string sql, IEnumerable<IDataParameter> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a single entity through THIS context, for a command handler
    /// that will go on to modify it in the same request. Deliberately
    /// separate from IQueryDbContext's read surface (which stays for pure
    /// queries), and deliberately constrained to Entity (a real, synthetic
    /// Id) rather than the broader IPersistable: reading via
    /// IQueryDbContext and then calling Alter()/Delete() here works fine
    /// for ordinary entities, but for one configured with a database-level
    /// concurrency token (Template, ProjectDocument — see Domain Data
    /// Model.md §3's xmin column), EF Core's xmin shadow property is
    /// tracked per-DbContext, not carried on the CLR object. Handing such
    /// an entity from IQueryDbContext's DbContext instance to this one
    /// silently drops the real xmin value, defaulting it to 0 — so every
    /// subsequent update looks like a stale-write conflict, even against a
    /// row nobody else touched. Any handler that both reads and modifies
    /// an xmin-tracked entity in the same request must read it through
    /// this method instead of IQueryDbContext.
    /// </summary>
    Task<TEntity?> FindAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : Entity;
}
