using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Repositories;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityDapper.Commands;

public sealed record UpdateSampleEntityDapperRequest(SampleEntityDefinition SampleEntity)
    : IMediatRCommandRequest<Result<int>>;
internal sealed class UpdateSampleEntityDapperHandler(ISampleEntityDapperCommandRepository repository)
    : IMediatRCommandHandler<UpdateSampleEntityDapperRequest, Result<int>>
{
    public async Task<Result<int>> Handle(
        UpdateSampleEntityDapperRequest request,
        CancellationToken cancellationToken)
    {
        var rowsAffected = await repository.UpdateAsync(request.SampleEntity, cancellationToken);
        return Result<int>.Success(rowsAffected);
    }
}
