using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// One of the 12 lifecycle-phase folders (00-vision-and-strategy … 11-handover)
/// used to classify both templates and project documents. Code is expected to
/// be the zero-padded folder prefix (e.g. "02-core-architecture") — ordering
/// relies on lexicographic sort of Code, no separate sort-order column.
/// </summary>
public sealed class Category : Entity
{
    public string Code { get; private set; }
    public string Name { get; private set; }

    public Category(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code is required.", nameof(code));
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Code = code;
        Name = name;
    }
}
