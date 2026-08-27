namespace ArchitectureToolkit.Application.Contracts.Users;

public sealed record UserDto(Guid Id, string Name, string Email, string SystemRole);
