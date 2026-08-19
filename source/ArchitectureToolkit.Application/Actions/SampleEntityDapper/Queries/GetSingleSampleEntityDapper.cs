using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Repositories;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityDapper.Queries;

public sealed record GetSingleSampleEntityDapperRequest(int Id)
    : IMediatRQueryRequest<Result<SampleEntityDefinition>>;
internal sealed class GetSingleSampleEntityDapperHandler(
    ISampleEntityDapperQueryRepository repository
    ) : IMediatRQueryHandler<GetSingleSampleEntityDapperRequest, Result<SampleEntityDefinition>>
{
    public async Task<Result<SampleEntityDefinition>> Handle(
        GetSingleSampleEntityDapperRequest request,
        CancellationToken cancellationToken)
    {
        var response = await repository.GetByIdAsync(request.Id, cancellationToken);
        return response is null ? Result<SampleEntityDefinition>.Failure("SampleEntityDapper not found.", ResultErrorType.NotFound) : Result<SampleEntityDefinition>.Success(response);
    }
}
