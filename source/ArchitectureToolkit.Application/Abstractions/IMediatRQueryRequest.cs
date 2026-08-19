using MediatR;

namespace ArchitectureToolkit.Application.Abstractions;

public interface IMediatRQueryRequest<out TResponse> : IRequest<TResponse>;
