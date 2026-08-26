using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.Exceptions;
using ArchitectureToolkit.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;

namespace ArchitectureToolkit.Persistence.Contexts;

public sealed class CommandDbContext(DbContextOptions<CommandDbContext> options)
    : BaseDbContext<CommandDbContext>(options), ICommandDbContext, IUnitOfWork
{
    public void Insert<TEntity>(TEntity entity) where TEntity : class, IPersistable
    {
        Set<TEntity>().Add(entity);
    }

    public void InsertRange<TEntity>(IReadOnlyCollection<TEntity> entities) where TEntity : class, IPersistable
    {
        Set<TEntity>().AddRange(entities);
    }

    public void Alter<TEntity>(TEntity entity) where TEntity : class, IPersistable
    {
        Set<TEntity>().Update(entity);
    }

    public void Delete<TEntity>(TEntity entity) where TEntity : class, IPersistable
    {
        Set<TEntity>().Remove(entity);
    }

    public Task<int> ExecuteSqlAsync(string sql, IEnumerable<IDataParameter> parameters, CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
    }

    public Task<TEntity?> FindAsync<TEntity>(Guid id, CancellationToken cancellationToken = default)
        where TEntity : Entity
    {
        return Set<TEntity>().SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    /// <summary>
    /// Translates EF Core's DbUpdateConcurrencyException (the xmin-based
    /// database-level concurrency guard — Domain Data Model.md §3) into
    /// Domain's own RevisionConflictException, so every caller only ever
    /// needs to catch one exception type regardless of whether the
    /// conflict was caught by RevisionHistory{T}'s in-memory check or by
    /// the database race the in-memory check can't see — exactly the
    /// unification RevisionConflictException's own doc comment describes.
    /// Application can't reference EF Core directly (Clean Architecture),
    /// so this translation has to happen here, the one place that can see
    /// both exception types. The specific expected/actual revision Ids
    /// aren't available from a raw DbUpdateConcurrencyException, so those
    /// are passed as null — RevisionConflictException already renders a
    /// sensible message either way.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new RevisionConflictException(null, null);
        }
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);
        return new EfCoreUnitOfWorkTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
