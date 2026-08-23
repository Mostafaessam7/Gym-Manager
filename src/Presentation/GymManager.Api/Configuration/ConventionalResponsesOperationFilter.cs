using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GymManager.Api.Configuration;

/// <summary>
/// Documents the standard response codes every endpoint in this API can realistically return, based on
/// simple, verifiable structural signals (whether the action allows anonymous access, whether its route has
/// an id-shaped path parameter, its HTTP verb) — purely for Swagger/OpenAPI output. This is a hand-written
/// alternative to ASP.NET Core's built-in <c>[ApiConventionType(typeof(DefaultApiConventions))]</c>, which
/// was tried first but empirically produced no effect in this project (verified by
/// <c>SwaggerConventionTests</c> failing against it), likely because most actions here take an extra
/// <c>CancellationToken</c> parameter the convention-matcher doesn't structurally match against. This filter
/// changes nothing about runtime behavior — every action still returns whatever its own
/// <c>Result</c>/<c>ToProblemDetails()</c> logic decides; it only adds documentation Swashbuckle wouldn't
/// otherwise infer, and never overwrites a response code an action already documents explicitly.
/// </summary>
public sealed class ConventionalResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // MethodInfo is null for any ApiDescription that doesn't resolve to a single reflectable method —
        // not reachable today (this API only registers MapControllers()/MapHealthChecks(), and health
        // checks aren't enumerated by ApiExplorer), but a future Minimal API endpoint added without an
        // explicit named delegate would hit this. Skip documenting that operation rather than throwing.
        if (context.MethodInfo is null)
            return;

        var allowsAnonymous = context.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
            || (context.MethodInfo.DeclaringType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ?? false);

        if (!allowsAnonymous)
        {
            AddIfMissing(operation, "401", "Unauthorized — the request is missing a valid bearer access token.");
            AddIfMissing(operation, "403", "Forbidden — the caller lacks the permission this endpoint requires.");
        }

        var httpMethod = context.ApiDescription.HttpMethod;
        var hasIdRouteParameter = context.ApiDescription.RelativePath?.Contains('{') == true;

        if (hasIdRouteParameter && httpMethod is "GET" or "PUT" or "DELETE")
            AddIfMissing(operation, "404", "Not Found — no resource exists with the given id.");

        if (httpMethod is "POST" or "PUT")
            AddIfMissing(operation, "400", "Bad Request — the request failed validation.");
    }

    private static void AddIfMissing(OpenApiOperation operation, string statusCode, string description)
    {
        if (!operation.Responses.ContainsKey(statusCode))
            operation.Responses[statusCode] = new OpenApiResponse { Description = description };
    }
}
