using MediatR;

namespace ArchitectureToolkit.Application.Abstractions;

public interface IMediatRCommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
    where TCommand : IMediatRCommandRequest<TResponse>;
