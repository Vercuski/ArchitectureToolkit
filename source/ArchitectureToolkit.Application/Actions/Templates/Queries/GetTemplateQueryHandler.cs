using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

public sealed class GetTemplateQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetTemplateQuery, Result<TemplateDetailDto>>
{
    public async Task<Result<TemplateDetailDto>> Handle(GetTemplateQuery request, CancellationToken cancellationToken)
    {
        var templateQuery = queryDbContext.Set<Template>().Where(t => t.Id == request.TemplateId);
        var template = await queryDbContext.SingleOrDefaultAsync(templateQuery, cancellationToken);

        if (template is null)
        {
            return Result<TemplateDetailDto>.Failure("Template not found.", ResultErrorType.NotFound);
        }

        if (template.CurrentRevisionId is null || template.CurrentVersion is null)
        {
            // Not reachable through the API (CreateTemplateCommand always
            // creates the first revision atomically) — a defensive guard
            // against a manually-inserted, incomplete row.
            return Result<TemplateDetailDto>.Failure(
                "Template has no revisions.", ResultErrorType.NotFound);
        }

        var revisionQuery = queryDbContext.Set<TemplateRevision>()
            .Where(r => r.Id == template.CurrentRevisionId);
        var revision = await queryDbContext.SingleOrDefaultAsync(revisionQuery, cancellationToken);

        if (revision is null)
        {
            // Referential integrity should make this impossible — a
            // defensive guard against a corrupt/inconsistent database.
            return Result<TemplateDetailDto>.Failure(
                $"Template '{template.Id}' references a missing revision '{template.CurrentRevisionId}'.",
                ResultErrorType.NotFound);
        }

        return Result<TemplateDetailDto>.Success(new TemplateDetailDto(
            template.Id,
            template.CategoryId,
            template.Name,
            template.CurrentVersion.Value.ToString(),
            revision.Id,
            revision.Content));
    }
}
