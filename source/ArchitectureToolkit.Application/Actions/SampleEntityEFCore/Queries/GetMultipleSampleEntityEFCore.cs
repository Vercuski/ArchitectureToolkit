using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityEFCore.Queries;

public sealed class GetMultipleSampleEntityEFCoresRequest
    : IMediatRQueryRequest<Result<List<SampleEntityDefinition>>>;
internal sealed class GetMultipleSampleEntityEFCoresHandler(IQueryDbContext queryDbContext)
    : IMediatRQueryHandler<GetMultipleSampleEntityEFCoresRequest, Result<List<SampleEntityDefinition>>>
{
    public async Task<Result<List<SampleEntityDefinition>>> Handle(
        GetMultipleSampleEntityEFCoresRequest request,
        CancellationToken cancellationToken)
    {
        List<SampleEntityDefinition> response = await queryDbContext.ToListAsync(
            queryDbContext.Set<SampleEntityDefinition>(), cancellationToken);
        return Result<List<SampleEntityDefinition>>.Success(response);
    }
}
