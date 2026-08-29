using GymManager.Api.BackgroundJobs;
using GymManager.Api.Configuration;
using GymManager.Api.Extensions;
using GymManager.Api.Middleware;
using GymManager.Application;
using GymManager.Infrastructure;
using GymManager.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FeatureManagement;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Optional Azure Key Vault integration. Set KeyVault__Uri to pull secrets from a vault instead of
// (or on top of) environment variables. Off by default, so nothing changes for anyone not using
// Azure - and deliberately registered before SecretsValidator runs, so a value supplied by the
// vault counts as configured rather than tripping the placeholder check.
//
// DefaultAzureCredential resolves a managed identity in Azure, or `az login` locally.
//
// Key Vault secret names cannot contain ':', so they use '--' instead: a secret named
// "Jwt--SecretKey" maps onto the Jwt:SecretKey configuration entry.
var keyVaultUri = builder.Configuration["KeyVault:Uri"];

if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new Azure.Identity.DefaultAzureCredential());
}

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    // Defense-in-depth: appsettings' "Serilog:MinimumLevel:Override" section already sets this, but Serilog
    // (unlike Microsoft.Extensions.Logging's "Logging:LogLevel") ignores that section if it's ever missing
    // from configuration — this override guarantees EF Core's per-query Information logs (which include full
    // generated SQL text and column names) never reach the console/file sinks regardless of config drift.
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/gymmanager-.log", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers();

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddJwtAuthentication(builder.Configuration)
    .AddApiVersioningSupport()
    .AddSwaggerDocumentation()
    .AddObservability(builder.Configuration, builder.Environment)
    .AddApiRateLimiting(builder.Environment);

builder.Services.AddFeatureManagement(builder.Configuration.GetSection("FeatureManagement"));

builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    string[] supportedCultures = ["en-US", "es-ES", "ar-SA"];
    options.SetDefaultCulture(supportedCultures[0])
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

builder.Services.AddHostedService<MembershipExpiryBackgroundService>();
builder.Services.AddHostedService<MembershipExpiringSoonReminderBackgroundService>();
builder.Services.AddHostedService<LowStockDigestBackgroundService>();
builder.Services.AddHostedService<InvoiceDueReminderBackgroundService>();
builder.Services.AddHostedService<DailyClosingReportBackgroundService>();

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        // Required for the HttpOnly refresh cookie to work at all. The frontend
        // sends credentials: 'include', and without this header the browser
        // rejects the whole response before the app sees it - the symptom is
        // login simply failing, with no error from the server.
        //
        // Safe here because the origins are explicitly configured; credentials
        // are forbidden with AllowAnyOrigin.
        .AllowCredentials());
});

if (!builder.Environment.IsDevelopment() && !builder.Environment.IsEnvironment("Testing"))
    SecretsValidator.EnsureProductionSecretsAreConfigured(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
    await DataSeeder.SeedAsync(app.Services);

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gym Manager API v1"));
}
else
{
    app.UseHsts();
}

app.UseRequestLocalization();
app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks are exempt from the global rate limiter (found via an actual load test, not assumed): the
// same 100-req/min-per-IP budget applied to every other endpoint also covered these by default, so a burst
// of *legitimate* API traffic from one client could exhaust the budget and start failing this container's
// own Docker HEALTHCHECK (or an external load balancer's liveness probe) purely as a side effect — causing
// an orchestrator to kill/restart a perfectly healthy container. Liveness/readiness probes are operationally
// critical, not a user-facing abuse surface, so they should never be subject to abuse-prevention throttling.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).DisableRateLimiting();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).DisableRateLimiting();

app.Run();

/// <summary>Explicit Program entry point, exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program;
