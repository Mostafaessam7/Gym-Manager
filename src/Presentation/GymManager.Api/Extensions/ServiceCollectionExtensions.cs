using System.Text;
using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Threading.RateLimiting;

namespace GymManager.Api.Extensions;

/// <summary>Wires up presentation-layer cross-cutting concerns: auth, versioning, Swagger, health, telemetry.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }

    public static IServiceCollection AddApiVersioningSupport(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // Several controllers declare a nested request DTO with the same short name (e.g.
            // `UpdatePlanRequest` on both MembershipPlansController and NutritionController) — Swashbuckle's
            // default schemaId is just the type name, so two such DTOs collide and blow up schema generation
            // with a 500 on /swagger/v1/swagger.json. Disambiguate using the declaring controller name too.
            options.CustomSchemaIds(type => type.DeclaringType is not null
                ? $"{type.DeclaringType.Name}.{type.Name}"
                : type.Name);

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Gym Manager API",
                Version = "v1",
                Description = "Enterprise gym management REST API.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT access token.",
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
                    },
                    []
                },
            });
        });

        return services;
    }

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddHealthChecks()
            .AddSqlServer(
                configuration.GetConnectionString("GymManagerDatabase")!,
                name: "sql-server",
                tags: ["ready", "db"]);

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(environment.ApplicationName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddConsoleExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddConsoleExporter());

        return services;
    }

    /// <summary>Name of the stricter policy applied to unauthenticated auth endpoints (login, register,
    /// password reset, 2FA challenge) via <c>[EnableRateLimiting(AuthRateLimitPolicy)]</c> — these are the
    /// endpoints a credential-stuffing/brute-force/email-bombing attempt would target, so they get a much
    /// tighter per-IP budget than the generous global limit meant for normal authenticated API traffic.</summary>
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IHostEnvironment environment)
    {
        // The integration test suite drives many rapid auth requests from the same loopback address within
        // the same window (login lockout, password-reset flows, etc.) — a real per-IP throttle would make
        // those tests flaky rather than actually testing anything, so it's relaxed to a no-op ceiling here,
        // the same way SecretsValidator is skipped outside Development/Testing rather than weakened globally.
        var isTesting = environment.IsEnvironment("Testing");

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = isTesting ? int.MaxValue : 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));

            options.AddPolicy(AuthRateLimitPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = isTesting ? int.MaxValue : 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }
}
