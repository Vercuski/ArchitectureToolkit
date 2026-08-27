using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

public sealed class ListTemplateRevisionsQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListTemplateRevisionsQuery, Result<IReadOnlyCollection<TemplateRevisionDto>>>
{
    public async Task<Result<IReadOnlyCollection<TemplateRevisionDto>>> Handle(
        ListTemplateRevisionsQuery request, CancellationToken cancellationToken)
    {
        var templateQuery = queryDbContext.Set<Template>().Where(t => t.Id == request.TemplateId);
        var template = await queryDbContext.SingleOrDefaultAsync(templateQuery, cancellationToken);

        if (template is null)
        {
            return Result<IReadOnlyCollection<TemplateRevisionDto>>.Failure(
                "Template not found.", ResultErrorType.NotFound);
        }

        var revisionsQuery = queryDbContext.Set<TemplateRevision>()
            .Where(r => r.TemplateId == request.TemplateId);
        var revisions = await queryDbContext.ToListAsync(revisionsQuery, cancellationToken);

        var summaries = revisions
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new TemplateRevisionDto(
                r.Id, r.TemplateId, r.Version.ToString(), r.BumpType?.ToString(), r.AuthorId, r.CreatedAt))
            .ToList();

        return Result<IReadOnlyCollection<TemplateRevisionDto>>.Success(summaries);
    }
}
