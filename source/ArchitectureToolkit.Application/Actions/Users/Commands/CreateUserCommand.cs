using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Users;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users.Commands;

/// <summary>
/// Admin-provisions a new user (ADR-0018) — architect-only, self-hosted
/// deployments only (see IIdentityAccountService.SupportsPasswordAccounts).
/// Creates the domain USER row directly with the given SystemRole and
/// sends (or surfaces, if email isn't configured/fails) a link the new
/// person uses to set their own password. Deliberately collects only
/// email + role, not a name — see CreateUserCommandHandler for how the
/// domain USER's required Name is derived in the meantime.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record CreateUserCommand(Guid CallerUserId, string Email, SystemRole SystemRole)
    : IMediatRCommandRequest<Result<CreateUserResult>>;
