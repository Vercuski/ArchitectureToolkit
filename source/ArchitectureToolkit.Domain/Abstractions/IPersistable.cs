namespace ArchitectureToolkit.Domain.Abstractions;

/// <summary>
/// Marker interface for anything ICommandDbContext/IQueryDbContext can
/// track and persist. IEntity extends this, so every existing Entity
/// subtype satisfies it automatically — this exists specifically for
/// PROJECT_MEMBER, which deliberately has no synthetic Id (its primary key
/// is the composite (ProjectId, UserId), matching the ERD) and therefore
/// cannot implement IEntity, but still needs to flow through the same
/// Insert/Alter/Delete/Set surface as every other persisted type.
/// </summary>
public interface IPersistable;
