using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Users;

namespace ArchitectureToolkit.Application.Actions.Users.Commands;

/// <summary>
/// Sets a target user's active status (ADR-0017). Authorized to callers
/// with SystemRole.Architect only — mirrors PromoteUserCommand's
/// authorization shape exactly, since both are User Management actions
/// gated the same way.
/// </summary>
/// <param name="CallerUserId">
/// The Id of the authenticated user issuing this command — resolved by the
/// API layer from the caller's validated principal, not supplied by the
/// caller as an arbitrary claim. Used solely for the architect-only
/// authorization check below.
/// </param>
/// <param name="TargetUserId">The user whose active status is being changed.</param>
/// <param name="IsActive">
/// The status to set. May equal the target's current status (a no-op
/// success). Setting this to false on the last remaining active Architect
/// is refused — see SetUserActiveStatusCommandHandler — the same guard
/// PromoteUserCommandHandler applies when demoting a SystemRole, now
/// counting only active Architects (ADR-0017).
/// </param>
public sealed record SetUserActiveStatusCommand(Guid CallerUserId, Guid TargetUserId, bool IsActive)
    : IMediatRCommandRequest<Result<UserManagementDto>>;
