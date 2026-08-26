using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Templates;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Templates.Queries;

public sealed class ListTemplatesQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListTemplatesQuery, Result<IReadOnlyCollection<TemplateSummaryDto>>>
{
    public async Task<Result<IReadOnlyCollection<TemplateSummaryDto>>> Handle(
        ListTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await queryDbContext.ToListAsync(queryDbContext.Set<Template>(), cancellationToken);

        var summaries = templates
            // A Template with no CurrentVersion has no revisions yet — not
            // possible through the API (CreateTemplateCommand always
            // creates the first revision atomically), but defensively
            // excluded rather than shown with a made-up version number.
            .Where(t => t.CurrentVersion is not null)
            .Select(t => new TemplateSummaryDto(t.Id, t.CategoryId, t.Name, t.CurrentVersion!.Value.ToString()))
            .ToList();

        return Result<IReadOnlyCollection<TemplateSummaryDto>>.Success(summaries);
    }
}
