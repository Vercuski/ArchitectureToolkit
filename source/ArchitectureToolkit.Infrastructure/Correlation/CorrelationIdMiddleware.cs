using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ArchitectureToolkit.Infrastructure.Correlation;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(
        HttpContext context,
        CorrelationIdAccessor accessor,
        ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString();

        accessor.Set(correlationId);

        // Set before next() runs — response headers become read-only once the body starts writing.
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }
}
