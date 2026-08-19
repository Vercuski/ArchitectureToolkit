using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Repositories;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityDapper.Commands;

public sealed record CreateSampleEntityDapperRequest(SampleEntityDefinition SampleEntity)
    : IMediatRCommandRequest<Result<int>>;
internal sealed class CreateSampleEntityDapperHandler(ISampleEntityDapperCommandRepository repository)
    : IMediatRCommandHandler<CreateSampleEntityDapperRequest, Result<int>>
{
    public async Task<Result<int>> Handle(
        CreateSampleEntityDapperRequest request,
        CancellationToken cancellationToken)
    {
        var rowsAffected = await repository.CreateAsync(request.SampleEntity, cancellationToken);
        return Result<int>.Success(rowsAffected);
    }
}
