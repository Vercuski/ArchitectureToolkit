using ArchitectureToolkit.Domain.Abstractions;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// An authenticated individual — the atomic unit of authorship and project
/// membership. SystemRole gates template-library governance, independent of
/// any project (ADR-0006).
/// </summary>
public sealed class User : Entity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public SystemRole SystemRole { get; private set; }

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
    }

    /// <summary>
    /// Promotes this user to Architect, granting template-library governance
    /// rights (ADR-0006). Used by the first-login bootstrap flow (ADR-0009)
    /// and by PromoteUserCommand for subsequent promotions.
    /// </summary>
    public void PromoteToArchitect()
    {
        SystemRole = SystemRole.Architect;
    }
}
