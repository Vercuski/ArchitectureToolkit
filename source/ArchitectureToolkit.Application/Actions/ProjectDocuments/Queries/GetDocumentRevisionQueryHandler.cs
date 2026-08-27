using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

public sealed class GetDocumentRevisionQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetDocumentRevisionQuery, Result<DocumentRevisionDetailDto>>
{
    public async Task<Result<DocumentRevisionDetailDto>> Handle(
        GetDocumentRevisionQuery request, CancellationToken cancellationToken)
    {
        var revisionQuery = queryDbContext.Set<DocumentRevision>()
            .Where(r => r.Id == request.RevisionId && r.DocumentId == request.DocumentId);
        var revision = await queryDbContext.SingleOrDefaultAsync(revisionQuery, cancellationToken);

        if (revision is null)
        {
            return Result<DocumentRevisionDetailDto>.Failure("Revision not found.", ResultErrorType.NotFound);
        }

        var documentQuery = queryDbContext.Set<ProjectDocument>().Where(d => d.Id == request.DocumentId);
        var document = await queryDbContext.SingleOrDefaultAsync(documentQuery, cancellationToken);

        if (document is null)
        {
            // Referential integrity should make this impossible if the
            // revision lookup above succeeded — a defensive guard.
            return Result<DocumentRevisionDetailDto>.Failure("Revision not found.", ResultErrorType.NotFound);
        }

        var membershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == document.ProjectId && pm.UserId == request.CallerUserId);
        var membership = await queryDbContext.SingleOrDefaultAsync(membershipQuery, cancellationToken);

        if (membership is null)
        {
            return Result<DocumentRevisionDetailDto>.Failure("Revision not found.", ResultErrorType.NotFound);
        }

        return Result<DocumentRevisionDetailDto>.Success(new DocumentRevisionDetailDto(
            revision.Id, revision.DocumentId, revision.Version.ToString(),
            revision.BumpType?.ToString(), revision.AuthorId, revision.CreatedAt, revision.Content));
    }
}
