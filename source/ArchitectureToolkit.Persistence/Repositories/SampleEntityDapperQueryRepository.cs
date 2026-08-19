using Dapper;
using ArchitectureToolkit.Application.Abstractions.ConnectionFactory;
using ArchitectureToolkit.Application.Abstractions.Repositories;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Persistence.Repositories;

public sealed class SampleEntityDapperQueryRepository(IDbReadOnlyConnectionFactory connectionFactory)
    : ISampleEntityDapperQueryRepository
{
    public async Task<List<SampleEntityDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT SampleId, SampleString, SampleBoolean, SampleInt, SampleDecimal FROM SampleTable";
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var response = await connection.QueryAsync<SampleEntityDefinition>(command);
        return [.. response];
    }

    public async Task<SampleEntityDefinition?> GetByIdAsync(int sampleId, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT SampleId, SampleString, SampleBoolean, SampleInt, SampleDecimal FROM SampleTable WHERE SampleId = @SampleId";
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { SampleId = sampleId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SampleEntityDefinition>(command);
    }
}
