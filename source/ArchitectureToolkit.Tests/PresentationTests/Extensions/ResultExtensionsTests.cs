using ArchitectureToolkit.Application.Abstractions;
using ArchitectureToolkit.Presentation.API.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Tests.PresentationTests.Extensions;

[TestFixture]
public class ResultExtensionsTests
{
    private sealed class TestController : ControllerBase;

    [Test]
    public void ToActionResult_Should_ReturnOk_WithValue_OnSuccess()
    {
        var controller = new TestController();
        var result = Result<string>.Success("value");

        var actionResult = result.ToActionResult(controller);

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo("value"));
    }

    [Test]
    public void ToActionResult_Should_ReturnNotFound_OnNotFoundFailure()
    {
        var controller = new TestController();
        var result = Result<string>.Failure("missing", ResultErrorType.NotFound);

        var actionResult = result.ToActionResult(controller);

        var notFoundResult = actionResult as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult!.Value, Is.EqualTo("missing"));
    }

    [Test]
    public void ToActionResult_Should_ReturnBadRequest_OnValidationFailure()
    {
        var controller = new TestController();
        var result = Result<string>.Failure("invalid", ResultErrorType.Validation);

        var actionResult = result.ToActionResult(controller);

        var badRequestResult = actionResult as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult!.Value, Is.EqualTo("invalid"));
    }

    [Test]
    public void ToActionResult_Should_ReturnConflict_OnConflictFailure()
    {
        var controller = new TestController();
        var result = Result<string>.Failure("conflict", ResultErrorType.Conflict);

        var actionResult = result.ToActionResult(controller);

        var conflictResult = actionResult as ConflictObjectResult;
        Assert.That(conflictResult, Is.Not.Null);
        Assert.That(conflictResult!.Value, Is.EqualTo("conflict"));
    }

    [Test]
    public void ToActionResult_Should_Return403_OnForbiddenFailure()
    {
        var controller = new TestController();
        var result = Result<string>.Failure("caller is not an architect", ResultErrorType.Forbidden);

        var actionResult = result.ToActionResult(controller);

        var objectResult = actionResult as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(objectResult!.StatusCode, Is.EqualTo(403));
            Assert.That(objectResult.Value, Is.EqualTo("caller is not an architect"));
        }
    }

    [Test]
    public void ToActionResult_WithProjection_Should_ReturnOk_WithProjectedValue_OnSuccess()
    {
        var controller = new TestController();
        var result = Result<int>.Success(42);

        var actionResult = result.ToActionResult(controller, value => $"number: {value}");

        var okResult = actionResult as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult!.Value, Is.EqualTo("number: 42"));
    }

    [Test]
    public void ToActionResult_WithProjection_Should_TreatNullValueSuccess_AsNotFound()
    {
        var controller = new TestController();
        var result = Result<string?>.Success(null);

        var actionResult = result.ToActionResult(controller, value => value);

        // Success with a null Value falls through to ToErrorResult, whose
        // switch defaults ErrorType (0 == NotFound) to a NotFound response —
        // matches "the original controller behavior of treating a success
        // with a null value as not-found" per this method's own doc comment.
        Assert.That(actionResult, Is.InstanceOf<NotFoundObjectResult>());
    }
}
