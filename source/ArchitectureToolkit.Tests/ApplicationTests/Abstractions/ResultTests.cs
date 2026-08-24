using ArchitectureToolkit.Application.Abstractions;

namespace ArchitectureToolkit.Tests.ApplicationTests.Abstractions;

[TestFixture]
public class ResultTests
{
    [Test]
    public void Success_Should_SetIsSuccessTrue_AndValue_AndNullError()
    {
        var result = Result<string>.Success("value");

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.EqualTo("value"));
        Assert.That(result.Error, Is.Null);
    }

    [TestCase(ResultErrorType.NotFound)]
    [TestCase(ResultErrorType.Validation)]
    [TestCase(ResultErrorType.Conflict)]
    [TestCase(ResultErrorType.Forbidden)]
    public void Failure_Should_SetIsSuccessFalse_AndError_AndErrorType_ForEveryErrorType(ResultErrorType errorType)
    {
        var result = Result<string>.Failure("something went wrong", errorType);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Value, Is.EqualTo(default(string)));
        Assert.That(result.Error, Is.EqualTo("something went wrong"));
        Assert.That(result.ErrorType, Is.EqualTo(errorType));
    }
}
