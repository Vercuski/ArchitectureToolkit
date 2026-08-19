using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Abstractions.Repositories;

/// <summary>
/// Write-side port for the Dapper persistence path. Defined in Application, implemented in
/// Persistence — mirrors <see cref="Context.ICommandDbContext"/> for the EF Core path so that
/// Application never depends on Dapper, raw SQL, or <see cref="System.Data.IDbConnection"/>.
/// </summary>
public interface ISampleEntityDapperCommandRepository
{
    Task<int> CreateAsync(SampleEntityDefinition entity, CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(SampleEntityDefinition entity, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int sampleId, CancellationToken cancellationToken = default);
}
