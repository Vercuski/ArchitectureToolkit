using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Users;

namespace ArchitectureToolkit.Application.Actions.Users.Queries;

/// <summary>
/// Lists every USER for the architect-only User Management tab (ADR-0017),
/// sorted alphabetically by email. Architect-only — unlike ListTemplatesQuery,
/// which any authenticated user may run — since this exposes every account
/// in the system, not shared library content.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record ListUsersQuery(Guid CallerUserId)
    : IMediatRQueryRequest<Result<IReadOnlyCollection<UserManagementDto>>>;
