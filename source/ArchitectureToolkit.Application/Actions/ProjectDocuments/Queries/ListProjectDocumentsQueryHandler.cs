using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.ProjectDocuments;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.ProjectDocuments.Queries;

public sealed class ListProjectDocumentsQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListProjectDocumentsQuery, Result<IReadOnlyCollection<ProjectDocumentSummaryDto>>>
{
    public async Task<Result<IReadOnlyCollection<ProjectDocumentSummaryDto>>> Handle(
        ListProjectDocumentsQuery request, CancellationToken cancellationToken)
    {
        var membershipQuery = queryDbContext.Set<ProjectMember>()
            .Where(pm => pm.ProjectId == request.ProjectId && pm.UserId == request.CallerUserId);
        var membership = await queryDbContext.SingleOrDefaultAsync(membershipQuery, cancellationToken);

        if (membership is null)
        {
            return Result<IReadOnlyCollection<ProjectDocumentSummaryDto>>.Failure(
                "Project not found.", ResultErrorType.NotFound);
        }

        var documentsQuery = queryDbContext.Set<ProjectDocument>().Where(d => d.ProjectId == request.ProjectId);
        var documents = await queryDbContext.ToListAsync(documentsQuery, cancellationToken);

        var summaries = documents
            // No CurrentVersion means no revisions yet — not reachable
            // through the API (see GetProjectDocumentQueryHandler), same
            // defensive exclusion as ListTemplatesQueryHandler.
            .Where(d => d.CurrentVersion is not null)
            .Select(d => new ProjectDocumentSummaryDto(
                d.Id, d.ProjectId, d.CategoryId, d.Title, d.CurrentVersion!.Value.ToString()))
            .ToList();

        return Result<IReadOnlyCollection<ProjectDocumentSummaryDto>>.Success(summaries);
    }
}
