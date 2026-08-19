using Dapper;
using ArchitectureToolkit.Application.Abstractions.ConnectionFactory;
using ArchitectureToolkit.Application.Abstractions.Repositories;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Persistence.Repositories;

public sealed class SampleEntityDapperCommandRepository(IDbWriteConnectionFactory connectionFactory)
    : ISampleEntityDapperCommandRepository
{
    public async Task<int> CreateAsync(SampleEntityDefinition entity, CancellationToken cancellationToken = default)
    {
        const string sql = "INSERT INTO SampleTable (SampleId, SampleString, SampleBoolean, SampleInt, SampleDecimal) " +
                            "VALUES (@SampleId, @SampleString, @SampleBoolean, @SampleInt, @SampleDecimal)";
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, entity, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    public async Task<int> UpdateAsync(SampleEntityDefinition entity, CancellationToken cancellationToken = default)
    {
        const string sql = "UPDATE SampleTable SET SampleString = @SampleString, SampleBoolean = @SampleBoolean, " +
                            "SampleInt = @SampleInt, SampleDecimal = @SampleDecimal WHERE SampleId = @SampleId";
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, entity, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }

    public async Task<int> DeleteAsync(int sampleId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM SampleTable WHERE SampleId = @SampleId";
        using var connection = connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { SampleId = sampleId }, cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command);
    }
}
