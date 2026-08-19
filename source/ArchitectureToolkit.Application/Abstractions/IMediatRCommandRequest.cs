using MediatR;

namespace ArchitectureToolkit.Application.Abstractions;

public interface IMediatRCommandRequest<out TResponse>
    : IRequest<TResponse>;
