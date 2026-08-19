using System.Data;

namespace ArchitectureToolkit.Application.Abstractions.ConnectionFactory;

public interface IDbWriteConnectionFactory
{
    IDbConnection CreateConnection();
}
