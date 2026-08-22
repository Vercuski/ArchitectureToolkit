using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// Top-level container an architect creates. Currently just a name — expected
/// to grow more fields (Domain Data Model.md §2).
/// </summary>
public sealed class Project : Entity
{
    public string Name { get; private set; }

    public Project(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Name = name;
    }
}
