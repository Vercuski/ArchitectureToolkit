using System.Diagnostics.CodeAnalysis;

namespace ArchitectureToolkit.Domain.Abstractions;

[ExcludeFromCodeCoverage]
public abstract class Entity : IEntity
{
    public Guid Id { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
    }
}
