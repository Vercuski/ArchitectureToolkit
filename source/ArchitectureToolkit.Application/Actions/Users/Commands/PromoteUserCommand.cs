using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users.Commands;

/// <summary>
/// Promotes or demotes a target user's SystemRole (ADR-0009). Authorized to
/// callers with SystemRole.Architect only. Distinct from the bootstrap flow
/// (ADR-0009/ADR-0014, implemented in Persistence's IUserProvisioningService),
/// which handles the *first* user; this command handles every promotion or
/// demotion after that.
/// </summary>
/// <param name="CallerUserId">
/// The Id of the authenticated user issuing this command — resolved by the
/// API layer from the caller's validated principal, not supplied by the
/// caller as an arbitrary claim. Used solely for the architect-only
/// authorization check below.
/// </param>
/// <param name="TargetUserId">The user whose SystemRole is being changed.</param>
/// <param name="NewSystemRole">
/// The role to set on the target user. May equal the target's current role
/// (a no-op success), promote a Contributor to Architect, or demote an
/// Architect to Contributor — see PromoteUserCommandHandler for the guard
/// against demoting the last remaining Architect.
/// </param>
public sealed record PromoteUserCommand(Guid CallerUserId, Guid TargetUserId, SystemRole NewSystemRole)
    : IMediatRCommandRequest<Result<SystemRole>>;
