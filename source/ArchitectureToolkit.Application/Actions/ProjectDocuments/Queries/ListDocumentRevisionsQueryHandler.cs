using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

public sealed class ListDocumentRevisionsQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListDocumentRevisionsQuery, Result<IReadOnlyCollection<DocumentRevisionDto>>>
{
    public async Task<Result<IReadOnlyCollection<DocumentRevisionDto>>> Handle(
        ListDocumentRevisionsQuery request, CancellationToken cancellationToken)
    {
        var documentQuery = queryDbContext.Set<ProjectDocument>().Where(d => d.Id == request.DocumentId);
        var document = await queryDbContext.SingleOrDefaultAsync(documentQuery, cancellationToken);

        if (document is null)
        {
            return Result<IReadOnlyCollection<DocumentRevisionDto>>.Failure(
                "Document not found.", ResultErrorType.NotFound);
        }

        var membershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == document.ProjectId && pm.UserId == request.CallerUserId);
        var membership = await queryDbContext.SingleOrDefaultAsync(membershipQuery, cancellationToken);

        if (membership is null)
        {
            // NotFound, not Forbidden — same non-member-existence-hiding
            // reasoning as GetProjectDocumentQueryHandler.
            return Result<IReadOnlyCollection<DocumentRevisionDto>>.Failure(
                "Document not found.", ResultErrorType.NotFound);
        }

        var revisionsQuery = queryDbContext.Set<DocumentRevision>()
            .Where(r => r.DocumentId == request.DocumentId);
        var revisions = await queryDbContext.ToListAsync(revisionsQuery, cancellationToken);

        var summaries = revisions
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new DocumentRevisionDto(
                r.Id, r.DocumentId, r.Version.ToString(), r.BumpType?.ToString(), r.AuthorId, r.CreatedAt))
            .ToList();

        return Result<IReadOnlyCollection<DocumentRevisionDto>>.Success(summaries);
    }
}
