using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Presentation.API.Controllers.Requests;

public sealed record SetUserActiveStatusRequest(bool IsActive);

public sealed record CreateUserRequest(string Email, SystemRole SystemRole);
