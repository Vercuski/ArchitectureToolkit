using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

public sealed class GetTemplateRevisionQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetTemplateRevisionQuery, Result<TemplateRevisionDetailDto>>
{
    public async Task<Result<TemplateRevisionDetailDto>> Handle(
        GetTemplateRevisionQuery request, CancellationToken cancellationToken)
    {
        var revisionQuery = queryDbContext.Set<TemplateRevision>()
            .Where(r => r.Id == request.RevisionId && r.TemplateId == request.TemplateId);
        var revision = await queryDbContext.SingleOrDefaultAsync(revisionQuery, cancellationToken);

        if (revision is null)
        {
            return Result<TemplateRevisionDetailDto>.Failure("Revision not found.", ResultErrorType.NotFound);
        }

        return Result<TemplateRevisionDetailDto>.Success(new TemplateRevisionDetailDto(
            revision.Id, revision.TemplateId, revision.Version.ToString(),
            revision.BumpType?.ToString(), revision.AuthorId, revision.CreatedAt, revision.Content));
    }
}
