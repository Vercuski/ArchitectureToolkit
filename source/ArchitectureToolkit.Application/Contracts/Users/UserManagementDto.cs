namespace ArchitectureToolkit.Application.Contracts.Users;

/// <summary>
/// Row shape for the architect-only User Management tab (ADR-0017).
/// Deliberately narrower than UserDto — Name and SystemRole aren't columns
/// that tab shows, so they aren't carried here.
/// </summary>
public sealed record UserManagementDto(Guid Id, string Email, bool IsActive);
