using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Application.Contracts.Categories;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.Categories.Queries;

public sealed class ListCategoriesQueryHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<ListCategoriesQuery, Result<IReadOnlyCollection<CategoryDto>>>
{
    public async Task<Result<IReadOnlyCollection<CategoryDto>>> Handle(
        ListCategoriesQuery request, CancellationToken cancellationToken)
    {
        var categories = await queryDbContext.ToListAsync(queryDbContext.Set<Category>(), cancellationToken);

        var dtos = categories
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .Select(c => new CategoryDto(c.Id, c.Code, c.Name))
            .ToList();

        return Result<IReadOnlyCollection<CategoryDto>>.Success(dtos);
    }
}
