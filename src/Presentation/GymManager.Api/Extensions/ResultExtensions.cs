using System.Globalization;
using System.Resources;
using GymManager.SharedKernel.Results;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Extensions;

/// <summary>Maps a failed <see cref="Result"/> to the appropriate <see cref="ProblemDetails"/> HTTP response.</summary>
public static class ResultExtensions
{
    // Looked up by CurrentUICulture (set per-request by UseRequestLocalization from the Accept-Language
    // header) rather than injected as IStringLocalizer, so every existing `result.ToProblemDetails()` call
    // site across every controller keeps working unchanged. Only a representative subset of error codes has
    // a translated resource entry today — translating the full ~150-code catalog is a content task, not an
    // engineering one; codes without an entry fall back to the original English Error.Message.
    private static readonly ResourceManager ErrorMessagesResourceManager =
        new("GymManager.Api.Resources.ErrorMessages", typeof(ResultExtensions).Assembly);

    public static IActionResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("A successful result cannot be converted to a problem.");

        var statusCode = result.Error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        var localizedDetail = ErrorMessagesResourceManager.GetString(result.Error.Code, CultureInfo.CurrentUICulture);

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = result.Error.Code,
            Detail = localizedDetail ?? result.Error.Message,
        })
        {
            StatusCode = statusCode,
        };
    }
}
