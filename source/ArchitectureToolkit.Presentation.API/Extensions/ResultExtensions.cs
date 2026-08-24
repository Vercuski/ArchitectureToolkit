using ArchitectureToolkit.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectureToolkit.Presentation.API.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Maps a <see cref="Result{T}"/> directly to an <see cref="IActionResult"/>.
    /// On success, returns 200 OK with <c>result.Value</c> as-is.
    /// </summary>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
        return result.IsSuccess
            ? controller.Ok(result.Value)
            : result.ToErrorResult(controller);
    }

    /// <summary>
    /// Maps a <see cref="Result{T}"/> to an <see cref="IActionResult"/>, projecting the
    /// success value through <paramref name="onSuccess"/> (e.g. entity -&gt; DTO) before
    /// returning it via 200 OK. Mirrors the original controller behavior of treating a
    /// success with a null value as not-found.
    /// </summary>
    public static IActionResult ToActionResult<T, TResponse>(
        this Result<T> result,
        ControllerBase controller,
        Func<T, TResponse> onSuccess)
    {
        return result.IsSuccess && result.Value is not null
            ? controller.Ok(onSuccess(result.Value))
            : result.ToErrorResult(controller);
    }

    private static ObjectResult ToErrorResult<T>(this Result<T> result, ControllerBase controller)
    {
        return result.ErrorType switch
        {
            ResultErrorType.NotFound => controller.NotFound(result.Error),
            ResultErrorType.Validation => controller.BadRequest(result.Error),
            ResultErrorType.Conflict => controller.Conflict(result.Error),
            // StatusCode, not Forbid(): Forbid() triggers the configured auth
            // scheme's challenge/forbid handler (for cookie/OIDC-style auth
            // flows). This is a domain-level authorization failure with a
            // message body (e.g. "caller is not an architect"), not an
            // auth-scheme-level challenge.
            ResultErrorType.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, result.Error),
            _ => controller.Problem(result.Error)
        };
    }
}
