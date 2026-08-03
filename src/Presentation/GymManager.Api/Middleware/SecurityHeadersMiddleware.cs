namespace GymManager.Api.Middleware;

/// <summary>
/// Adds baseline hardening headers to every response. This is a pure JSON API with no server-rendered HTML,
/// so the policy is deliberately strict everywhere except Development, where Swagger UI's own inline
/// scripts/styles would otherwise be blocked by the same policy.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (!environment.IsDevelopment())
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        return next(context);
    }
}
