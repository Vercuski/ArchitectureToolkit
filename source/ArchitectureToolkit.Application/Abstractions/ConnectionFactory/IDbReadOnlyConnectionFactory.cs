using System.Data;

namespace ArchitectureToolkit.Application.Abstractions.ConnectionFactory;

public interface IDbReadOnlyConnectionFactory
{
    IDbConnection CreateConnection();
}
