using ArchitectureToolkit.Domain.Abstractions;
using System.Data;

namespace ArchitectureToolkit.Application.Abstractions.Context;

public interface ICommandDbContext
{
    void Insert<TEntity>(TEntity entity) where TEntity : Entity;
    void InsertRange<TEntity>(IReadOnlyCollection<TEntity> entities) where TEntity : Entity;
    void Alter<TEntity>(TEntity entity) where TEntity : Entity;
    void Delete<TEntity>(TEntity entity) where TEntity : Entity;
    int SaveChanges();
    Task<int> ExecuteSqlAsync(string sql, IEnumerable<IDataParameter> parameters, CancellationToken cancellationToken = default);
}
