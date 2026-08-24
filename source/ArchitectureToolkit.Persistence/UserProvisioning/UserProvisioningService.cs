using System.Security.Claims;
using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;
using ArchitectureToolkit.Domain.ValueObjects;

namespace ArchitectureToolkit.Persistence.UserProvisioning;

public sealed class UserProvisioningService(
    ICommandDbContext commandDbContext,
    IQueryDbContext queryDbContext,
    IUnitOfWork unitOfWork,
    ITemplateLibrarySource templateLibrarySource)
    : IUserProvisioningService
{
    public async Task<Result<User>> ResolveOrProvisionUserAsync(
        ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var subjectClaim = principal.FindFirst("sub") ?? principal.FindFirst(ClaimTypes.NameIdentifier);
        if (subjectClaim is null || string.IsNullOrWhiteSpace(subjectClaim.Value))
        {
            return Result<User>.Failure("Principal is missing a subject ('sub') claim.", ResultErrorType.Validation);
        }

        var externalSubjectId = subjectClaim.Value;
        var issuer = subjectClaim.Issuer;

        if (string.IsNullOrWhiteSpace(issuer) || issuer == ClaimsIdentity.DefaultIssuer)
        {
            // ClaimsIdentity.DefaultIssuer ("LOCAL AUTHORITY") is what .NET
            // assigns a Claim when no real issuer was set on it. Treating
            // that as a genuine issuer would silently defeat the
            // (issuer, external_subject_id) uniqueness key ADR-0004 relies
            // on — every such principal would collide on the same "issuer".
            return Result<User>.Failure(
                "Principal's subject claim has no real issuer.", ResultErrorType.Validation);
        }

        // ADR-0004: resolve via USER_IDENTITY(issuer, external_subject_id) first.
        var existingIdentityQuery = queryDbContext.Set<UserIdentity>()
            .Where(i => i.Issuer == issuer && i.ExternalSubjectId == externalSubjectId);
        var existingIdentity = await queryDbContext.SingleOrDefaultAsync(existingIdentityQuery, cancellationToken);

        if (existingIdentity is not null)
        {
            var existingUserQuery = queryDbContext.Set<User>().Where(u => u.Id == existingIdentity.UserId);
            var existingUser = await queryDbContext.SingleOrDefaultAsync(existingUserQuery, cancellationToken);

            if (existingUser is null)
            {
                // Referential integrity should make this impossible — a
                // defensive guard against a corrupt/inconsistent database,
                // not an expected path.
                return Result<User>.Failure(
                    $"USER_IDENTITY '{existingIdentity.Id}' references a missing USER '{existingIdentity.UserId}'.",
                    ResultErrorType.NotFound);
            }

            return Result<User>.Success(existingUser);
        }

        // Not found — JIT-provision (ADR-0004).
        var name = principal.FindFirst("name")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        var email = principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<User>.Failure("Principal is missing a name claim.", ResultErrorType.Validation);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<User>.Failure("Principal is missing an email claim.", ResultErrorType.Validation);
        }

        var providerLabel = ExtractProviderLabel(principal, issuer);

        // ADR-0009: the first-user check happens in the same transaction as
        // the insert, to narrow (not fully eliminate) the check-then-act
        // race window between two people logging in at the same instant on
        // a still-empty install.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var anyUserQuery = queryDbContext.Set<User>();
            var isFirstUser = (await queryDbContext.ToListAsync(anyUserQuery, cancellationToken)).Count == 0;
            var systemRole = isFirstUser ? SystemRole.Architect : SystemRole.Contributor;

            var newUser = new User(name, email, systemRole);
            commandDbContext.Insert(newUser);

            var newIdentity = new UserIdentity(newUser.Id, issuer, externalSubjectId, providerLabel);
            commandDbContext.Insert(newIdentity);

            if (isFirstUser)
            {
                // ADR-0014: seed the template library too, still inside this
                // same transaction — but only if TEMPLATE is also empty,
                // checked independently of the USER check per the ADR's own
                // pseudocode (in practice the two are correlated, but the
                // ADR specifies two separate checks, not one combined one).
                var anyTemplateQuery = queryDbContext.Set<Template>();
                var isTemplateLibraryEmpty =
                    (await queryDbContext.ToListAsync(anyTemplateQuery, cancellationToken)).Count == 0;

                if (isTemplateLibraryEmpty)
                {
                    await SeedTemplateLibraryAsync(newUser.Id, cancellationToken);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result<User>.Success(newUser);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// ADR-0014: reads the bundled template library and creates one
    /// CATEGORY per folder plus one TEMPLATE + first TEMPLATE_REVISION
    /// (seeded at 1.0.0 — RevisionHistory{T} enforces this regardless of
    /// bumpType on a first revision, per ADR-0013) per file, all authored
    /// by the newly-bootstrapped architect. No sentinel/system user.
    /// </summary>
    private async Task SeedTemplateLibraryAsync(Guid authorId, CancellationToken cancellationToken)
    {
        var categories = await templateLibrarySource.GetCategoriesAsync(cancellationToken);

        foreach (var categoryData in categories)
        {
            var category = new Category(categoryData.Code, categoryData.Name);
            commandDbContext.Insert(category);

            foreach (var templateData in categoryData.Templates)
            {
                var template = new Template(category.Id, templateData.Name);
                template.CreateRevision(null, null, templateData.Content, authorId);
                commandDbContext.Insert(template);
            }
        }
    }

    /// <summary>
    /// ADR-0004 doesn't specify exactly where the display-only
    /// provider_label comes from. Prefers a conventional "idp" claim some
    /// auth handlers populate; falls back to the issuer URL's host, which
    /// is always available and reasonably identifies the provider (e.g.
    /// "accounts.google.com").
    /// </summary>
    private static string ExtractProviderLabel(ClaimsPrincipal principal, string issuer)
    {
        var idpClaim = principal.FindFirst("idp")?.Value;
        if (!string.IsNullOrWhiteSpace(idpClaim))
        {
            return idpClaim;
        }

        return Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) ? issuerUri.Host : issuer;
    }
}
