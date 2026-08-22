using ArchitectureToolkit.Domain.Abstractions;

namespace ArchitectureToolkit.Domain.Entities;

/// <summary>
/// Links a User to one external identity — an (Issuer, ExternalSubjectId) pair
/// from a validated OIDC token. A plain join entity, not revision-tracked
/// (Domain Data Model.md §3). See ADR-0003, ADR-0004.
///
/// (Issuer, ExternalSubjectId) must be unique — enforced via a database index,
/// not here; the Domain layer alone can't check uniqueness across rows.
/// </summary>
public sealed class UserIdentity : Entity
{
    public Guid UserId { get; private set; }
    public string Issuer { get; private set; }
    public string ExternalSubjectId { get; private set; }
    public string ProviderLabel { get; private set; }
    public DateTime LinkedAt { get; private set; }

    public UserIdentity(Guid userId, string issuer, string externalSubjectId, string providerLabel)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException("Issuer is required.", nameof(issuer));
        }
        if (string.IsNullOrWhiteSpace(externalSubjectId))
        {
            throw new ArgumentException("ExternalSubjectId is required.", nameof(externalSubjectId));
        }
        if (string.IsNullOrWhiteSpace(providerLabel))
        {
            throw new ArgumentException("ProviderLabel is required.", nameof(providerLabel));
        }

        UserId = userId;
        Issuer = issuer;
        ExternalSubjectId = externalSubjectId;
        ProviderLabel = providerLabel;
        LinkedAt = DateTime.UtcNow;
    }
}
