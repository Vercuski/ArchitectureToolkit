using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityEFCore.Commands;

public sealed record CreateSampleEntityEFCoreRequest(SampleEntityDefinition SampleEntity)
    : IMediatRCommandRequest<Result<int>>;
internal sealed class CreateSampleEntityEFCoreHandler(ICommandDbContext commandDbContext)
    : IMediatRCommandHandler<CreateSampleEntityEFCoreRequest, Result<int>>
{
    public async Task<Result<int>> Handle(
        CreateSampleEntityEFCoreRequest request,
        CancellationToken cancellationToken)
    {
        commandDbContext.Insert(request.SampleEntity);
        int rowsAffected = commandDbContext.SaveChanges();
        return Result<int>.Success(rowsAffected);
    }
}
