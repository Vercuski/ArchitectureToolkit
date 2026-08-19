using MediatR;
using Microsoft.Extensions.Logging;

namespace ArchitectureToolkit.Application.Behaviors;

/// <summary>
/// Logs entry, successful completion, and failure of every MediatR command/query. Combined with
/// the ambient correlation-ID logging scope pushed by CorrelationIdMiddleware (Infrastructure),
/// this reconstructs the full "path" of a request through the CQRS pipeline without any handler
/// needing to know a correlation ID exists.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            logger.LogInformation("Handled {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{RequestName} failed", requestName);
            throw;
        }
    }
}
