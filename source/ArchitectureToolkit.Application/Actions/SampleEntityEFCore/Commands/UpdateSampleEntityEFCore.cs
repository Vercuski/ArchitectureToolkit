using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityEFCore.Commands;

public sealed record UpdateSampleEntityEFCoreRequest(SampleEntityDefinition SampleEntity)
    : IMediatRCommandRequest<Result<int>>;
internal sealed class UpdateSampleEntityEFCoreHandler(ICommandDbContext commandDbContext)
    : IMediatRCommandHandler<UpdateSampleEntityEFCoreRequest, Result<int>>
{
    public Task<Result<int>> Handle(
        UpdateSampleEntityEFCoreRequest request,
        CancellationToken cancellationToken)
    {
        commandDbContext.Alter(request.SampleEntity);
        int rowsAffected = commandDbContext.SaveChanges();
        return Task.FromResult(Result<int>.Success(rowsAffected));
    }
}
