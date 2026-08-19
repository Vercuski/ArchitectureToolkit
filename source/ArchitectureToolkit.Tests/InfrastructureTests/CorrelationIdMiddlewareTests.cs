using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ArchitectureToolkit.Infrastructure.Correlation;

namespace ArchitectureToolkit.Tests.InfrastructureTests;

[TestFixture]
public class CorrelationIdMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_Should_SetResponseHeader_AndMakeIdAvailableDownstream()
    {
        var accessor = new CorrelationIdAccessor();
        string? observedInsideNext = null;

        // The lambda below stands in for "the rest of the pipeline" — a MediatR handler or
        // repository call several layers deep. It reads the accessor with no parameter having
        // been passed to it, which is the "invisible" propagation this middleware exists for.
        var middleware = new CorrelationIdMiddleware(context =>
        {
            observedInsideNext = accessor.CorrelationId;
            return Task.CompletedTask;
        });

        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext, accessor, NullLogger<CorrelationIdMiddleware>.Instance);

        var headerValue = httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(headerValue, Is.Not.Empty);
            Assert.That(Guid.TryParse(headerValue, out _), Is.True);
            Assert.That(observedInsideNext, Is.EqualTo(headerValue));
        }
    }

    [Test]
    public async Task InvokeAsync_Should_ReuseIncomingCorrelationIdHeader_WhenPresent()
    {
        var accessor = new CorrelationIdAccessor();
        var httpContext = new DefaultHttpContext();
        var incomingId = Guid.NewGuid().ToString();
        httpContext.Request.Headers[CorrelationIdMiddleware.HeaderName] = incomingId;

        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(httpContext, accessor, NullLogger<CorrelationIdMiddleware>.Instance);

        var headerValue = httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString();
        Assert.That(headerValue, Is.EqualTo(incomingId));
    }
}
