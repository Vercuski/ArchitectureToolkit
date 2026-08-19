using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Application.Abstractions;

public interface IDomainMapper<out TEntity>
    where TEntity : Entity
{
    TEntity MapToDomain();
}
