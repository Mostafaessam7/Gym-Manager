using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Middleware;

/// <summary>
/// Last line of defense against unhandled exceptions. Expected failures should be surfaced through the
/// <c>Result</c> pattern instead — this middleware exists to turn genuine bugs and infrastructure faults
/// into a consistent <see cref="ProblemDetails"/> response instead of leaking stack traces.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                    ? exception.ToString()
                    : "Please contact support if the problem persists.",
                Instance = context.Request.Path,
            };

            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = problemDetails.Status.Value;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
