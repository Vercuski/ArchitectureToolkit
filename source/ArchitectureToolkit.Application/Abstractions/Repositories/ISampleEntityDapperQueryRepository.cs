using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Abstractions.Repositories;

/// <summary>
/// Read-side port for the Dapper persistence path. Defined in Application, implemented in
/// Persistence — mirrors <see cref="Context.IQueryDbContext"/> for the EF Core path so that
/// Application never depends on Dapper, raw SQL, or <see cref="System.Data.IDbConnection"/>.
/// </summary>
public interface ISampleEntityDapperQueryRepository
{
    Task<List<SampleEntityDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SampleEntityDefinition?> GetByIdAsync(int sampleId, CancellationToken cancellationToken = default);
}
