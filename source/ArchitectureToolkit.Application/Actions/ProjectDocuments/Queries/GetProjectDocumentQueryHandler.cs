using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

public sealed class GetProjectDocumentQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetProjectDocumentQuery, Result<ProjectDocumentDetailDto>>
{
    public async Task<Result<ProjectDocumentDetailDto>> Handle(
        GetProjectDocumentQuery request, CancellationToken cancellationToken)
    {
        var documentQuery = queryDbContext.Set<ProjectDocument>().Where(d => d.Id == request.DocumentId);
        var document = await queryDbContext.SingleOrDefaultAsync(documentQuery, cancellationToken);

        if (document is null)
        {
            return Result<ProjectDocumentDetailDto>.Failure("Document not found.", ResultErrorType.NotFound);
        }

        var membershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == document.ProjectId && pm.UserId == request.CallerUserId);
        var membership = await queryDbContext.SingleOrDefaultAsync(membershipQuery, cancellationToken);

        if (membership is null)
        {
            // NotFound, not Forbidden — same non-member-existence-hiding
            // reasoning as GetProjectQueryHandler.
            return Result<ProjectDocumentDetailDto>.Failure("Document not found.", ResultErrorType.NotFound);
        }

        if (document.CurrentRevisionId is null || document.CurrentVersion is null)
        {
            // Not reachable through the API (CreateProjectDocumentCommand
            // always creates the first revision atomically) — a
            // defensive guard against a manually-inserted, incomplete row.
            return Result<ProjectDocumentDetailDto>.Failure("Document has no revisions.", ResultErrorType.NotFound);
        }

        var revisionQuery = queryDbContext.Set<DocumentRevision>()
            .Where(r => r.Id == document.CurrentRevisionId);
        var revision = await queryDbContext.SingleOrDefaultAsync(revisionQuery, cancellationToken);

        if (revision is null)
        {
            // Referential integrity should make this impossible — a
            // defensive guard against a corrupt/inconsistent database.
            return Result<ProjectDocumentDetailDto>.Failure(
                $"Document '{document.Id}' references a missing revision '{document.CurrentRevisionId}'.",
                ResultErrorType.NotFound);
        }

        return Result<ProjectDocumentDetailDto>.Success(new ProjectDocumentDetailDto(
            document.Id,
            document.ProjectId,
            document.CategoryId,
            document.Title,
            document.CurrentVersion.Value.ToString(),
            revision.Id,
            document.SourceTemplateRevisionId,
            revision.Content));
    }
}
