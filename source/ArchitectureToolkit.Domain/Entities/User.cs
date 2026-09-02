using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// An authenticated individual — the atomic unit of authorship and project
/// membership. SystemRole gates template-library governance, independent of
/// any project (ADR-0006). IsActive gates API access entirely, independent
/// of SystemRole (ADR-0017) — enforced at
/// ApiControllerBase.ResolveCallerUserIdAsync, not here; this entity only
/// carries the flag.
/// </summary>
public sealed class User : Entity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public SystemRole SystemRole { get; private set; }
    public bool IsActive { get; private set; }

    public User(string name, string email, SystemRole systemRole)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        Name = name;
        Email = email;
        SystemRole = systemRole;
        IsActive = true;
    }

    /// <summary>
    /// Sets this user's system role directly. Used by PromoteUserCommand
    /// (ADR-0009), which can promote or demote a target user to either
    /// SystemRole — not just grant Architect.
    /// </summary>
    public void SetSystemRole(SystemRole newRole)
    {
        SystemRole = newRole;
    }

    /// <summary>
    /// Promotes this user to Architect, granting template-library governance
    /// rights (ADR-0006). Used by the first-login bootstrap flow (ADR-0009).
    /// A thin wrapper over <see cref="SetSystemRole"/> so the bootstrap
    /// flow's call site stays a specific, self-documenting method rather
    /// than every caller needing to know which role bootstrap grants.
    /// </summary>
    public void PromoteToArchitect()
    {
        SetSystemRole(SystemRole.Architect);
    }

    /// <summary>
    /// Sets this user's active status (ADR-0017). Used by
    /// SetUserActiveStatusCommand, architect-only. Setting this to false
    /// does not, by itself, revoke anything — ApiControllerBase is what
    /// actually denies API access to an inactive user on every subsequent
    /// request; this method only records the flag.
    /// </summary>
    public void SetActiveStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
