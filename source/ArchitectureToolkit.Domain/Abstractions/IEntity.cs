namespace ArchitectureToolkit.Domain.Abstractions;

public interface IEntity : IPersistable
{
    Guid Id { get; }
}
