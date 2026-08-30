using ArchitectureToolkit.Domain.Entities;
using System.Security.Claims;

namespace ArchitectureToolkit.Application.Abstractions;

/// <summary>
/// Resolves an authenticated principal to the corresponding USER, or
/// just-in-time provisions one if this identity has never authenticated
/// before (ADR-0003, ADR-0004). Application code depends only on this
/// interface — the concrete implementation lives in Persistence, not
/// Infrastructure (see ADR-0003 §3: it needs ICommandDbContext/IUnitOfWork,
/// which Infrastructure is walled off from by an enforced fitness test).
/// </summary>
public interface IUserProvisioningService
{
    /// <summary>
    /// Looks up USER_IDENTITY by the (issuer, external_subject_id) pair
    /// carried on <paramref name="principal"/>'s claims. If found, returns
    /// the linked USER. If not, provisions a new USER + USER_IDENTITY
    /// together inside a single transaction — the same transaction that
    /// also runs the first-user/first-template bootstrap checks from
    /// ADR-0009/ADR-0014 (first user becomes Architect; if TEMPLATE is
    /// also empty, the bundled template library is seeded, authored by
    /// that user).
    /// </summary>
    /// <param name="principal">
    /// A validated ClaimsPrincipal. The caller (Infrastructure's auth
    /// pipeline) is responsible for having already verified the
    /// token/session this principal was built from — this service trusts
    /// the claims it's given without re-validating them. It does still
    /// check that the issuer/subject claims are actually present — see the
    /// Validation failure case below.
    /// </param>
    /// <returns>
    /// Result&lt;User&gt;.Success with the resolved or newly-provisioned
    /// USER in every normal case. Result&lt;User&gt;.Failure with
    /// ResultErrorType.Validation only if <paramref name="principal"/> is
    /// missing the issuer or subject claim ADR-0004's USER_IDENTITY lookup
    /// requires — a validated token should always carry both, so this is a
    /// defensive case, not an expected one. Anything else that goes wrong
    /// (e.g. the database being unreachable) is a technical failure and
    /// surfaces as a thrown exception, not a Result.Failure — there's no
    /// other business-level "no user could be resolved" outcome for a
    /// validated principal.
    /// </returns>
    Task<Result<User>> ResolveOrProvisionUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
