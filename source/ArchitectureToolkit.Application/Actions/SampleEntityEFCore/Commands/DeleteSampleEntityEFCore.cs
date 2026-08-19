using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Application.Abstractions.Context;
using ArchitectureToolkit.Domain.Entities;

namespace ArchitectureToolkit.Application.Actions.SampleEntityEFCore.Commands;

public sealed record DeleteSampleEntityEFCoreRequest(SampleEntityDefinition Entity)
    : IMediatRCommandRequest<Result<int>>;
internal sealed class DeleteSampleEntityEFCoreHandler(ICommandDbContext commandDbContext)
    : IMediatRCommandHandler<DeleteSampleEntityEFCoreRequest, Result<int>>
{
    public Task<Result<int>> Handle(
        DeleteSampleEntityEFCoreRequest request,
        CancellationToken cancellationToken)
    {
        commandDbContext.Delete(request.Entity);
        int rowsAffected = commandDbContext.SaveChanges();
        return Task.FromResult(Result<int>.Success(rowsAffected));
    }
}
