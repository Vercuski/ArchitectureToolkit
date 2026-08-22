using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Abstractions;

/// <summary>
/// Contract required of any revision type composed into a <see cref="RevisionHistory{TRevision}"/>
/// (ADR-0007) — currently TemplateRevision and DocumentRevision.
/// </summary>
public interface IRevision : IEntity
{
    VersionNumber Version { get; }
}
