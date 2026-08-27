using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Contracts.Users;

namespace ArchitectureToolkit.Application.Actions.Users.Queries;

/// <summary>
/// Returns the caller's own resolved profile — specifically, their domain
/// USER.Id, which nothing else in the UI can otherwise discover. Without
/// this, a project Owner has no way to learn another person's USER.Id to
/// pass to AddProjectMemberCommand, since that resolution only ever
/// happens server-side (IUserProvisioningService, ADR-0003/0004); a
/// person's own Id is only visible to them via this endpoint, to then
/// share with whoever needs to add them to a project.
/// </summary>
/// <param name="CallerUserId">Resolved by the API layer, not caller-supplied.</param>
public sealed record GetCurrentUserQuery(Guid CallerUserId) : IMediatRQueryRequest<Result<UserDto>>;
