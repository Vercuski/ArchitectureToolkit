using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ArchitectureToolkit.Infrastructure.Correlation;
using ArchitectureToolkit.Infrastructure.Exceptions;
using System.Text.Json;

namespace ArchitectureToolkit.Tests.InfrastructureTests;

[TestFixture]
public class GlobalExceptionHandlerTests
{
    [Test]
    public async Task TryHandleAsync_Should_IncludeCorrelationId_InProblemDetailsResponse()
    {
        var accessor = new CorrelationIdAccessor();
        var correlationId = Guid.NewGuid().ToString();
        accessor.Set(correlationId);

        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance, accessor);

        var httpContext = new DefaultHttpContext();
        using var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(handled, Is.True);
            Assert.That(httpContext.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }

        responseBody.Seek(0, SeekOrigin.Begin);
        using var document = await JsonDocument.ParseAsync(responseBody);

        // ProblemDetails.Extensions is [JsonExtensionData], so correlationId is flattened onto
        // the root object rather than nested — this is what an API consumer actually receives.
        var returnedCorrelationId = document.RootElement.GetProperty("correlationId").GetString();

        Assert.That(returnedCorrelationId, Is.EqualTo(correlationId));
    }
}
