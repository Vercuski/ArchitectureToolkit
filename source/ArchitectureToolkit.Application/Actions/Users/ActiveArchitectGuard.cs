using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Application.Actions.Users;

/// <summary>
/// The single "at least one active Architect must remain" invariant shared
/// by PromoteUserCommandHandler (demoting a SystemRole) and
/// SetUserActiveStatusCommandHandler (deactivating a user) — ADR-0017
/// explicitly calls out the risk of these two guards drifting apart if
/// each handler queried this independently, so both call this instead.
/// </summary>
internal static class ActiveArchitectGuard
{
    /// <summary>
    /// True if <paramref name="target"/> is currently an active Architect
    /// and removing that status (via demotion or deactivation — the caller
    /// decides which) would leave zero users who are both
    /// SystemRole.Architect and IsActive. False for any user who isn't
    /// currently an active Architect — deactivating or demoting an
    /// already-inactive Architect never changes the active-architect
    /// count, so it's never blocked by this guard regardless of how many
    /// other architects exist.
    /// </summary>
    public static async Task<bool> WouldRemoveLastActiveArchitectAsync(
        IQueryDbContext queryDbContext, User target, CancellationToken cancellationToken)
    {
        if (target.SystemRole != SystemRole.Architect || !target.IsActive)
        {
            return false;
        }

        var activeArchitectsQuery = queryDbContext.Set<User>()
            .Where(u => u.SystemRole == SystemRole.Architect && u.IsActive);
        var activeArchitects = await queryDbContext.ToListAsync(activeArchitectsQuery, cancellationToken);

        return activeArchitects.Count <= 1;
    }
}
