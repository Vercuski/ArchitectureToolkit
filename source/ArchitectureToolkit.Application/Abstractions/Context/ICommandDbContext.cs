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
}
