using GymManager.Infrastructure.Persistence.Seed;

namespace GymManager.Api.Configuration;

/// <summary>
/// Fails startup fast outside Development/Testing if the well-known dev-time placeholder secrets from
/// <c>appsettings.json</c> (JWT signing key, SQL Server password) are still in effect. Those placeholders
/// are intentionally checked into source control for local/docker-compose convenience, so the only safe
/// production posture is to guarantee they can never reach a real deployment silently — real values must be
/// supplied via environment variables (e.g. <c>Jwt__SecretKey</c>, <c>ConnectionStrings__GymManagerDatabase</c>)
/// or a secrets manager (Azure Key Vault, AWS Secrets Manager, etc.) wired into configuration.
/// </summary>
public static class SecretsValidator
{
    private const string PlaceholderJwtSecretKey = "CHANGE_THIS_TO_A_SECURE_RANDOM_SECRET_OF_AT_LEAST_32_CHARACTERS";
    private const string PlaceholderDbPassword = "Your_password123!";
    private const string PlaceholderStripeSecretKeyMarker = "CHANGE_THIS_TO_YOUR_STRIPE_TEST_SECRET_KEY";
    private const string PlaceholderStripeWebhookSecretMarker = "CHANGE_THIS_TO_YOUR_STRIPE_WEBHOOK_SIGNING_SECRET";

    public static void EnsureProductionSecretsAreConfigured(IConfiguration configuration)
    {
        var problems = new List<string>();

        var jwtSecretKey = configuration["Jwt:SecretKey"];
        if (string.IsNullOrWhiteSpace(jwtSecretKey) || jwtSecretKey == PlaceholderJwtSecretKey || jwtSecretKey.Length < 32)
            problems.Add("Jwt:SecretKey is missing, too short, or still the checked-in placeholder. Set it via the Jwt__SecretKey environment variable or a secrets manager.");

        var connectionString = configuration.GetConnectionString("GymManagerDatabase");
        if (string.IsNullOrWhiteSpace(connectionString) || connectionString.Contains(PlaceholderDbPassword, StringComparison.OrdinalIgnoreCase))
            problems.Add("ConnectionStrings:GymManagerDatabase is missing or still uses the checked-in placeholder password. Set it via the ConnectionStrings__GymManagerDatabase environment variable or a secrets manager.");

        var stripeSecretKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrWhiteSpace(stripeSecretKey) && stripeSecretKey.Contains(PlaceholderStripeSecretKeyMarker, StringComparison.OrdinalIgnoreCase))
            problems.Add("Stripe:SecretKey is still the checked-in placeholder. Set it via the Stripe__SecretKey environment variable or a secrets manager.");

        var stripeWebhookSecret = configuration["Stripe:WebhookSecret"];
        if (!string.IsNullOrWhiteSpace(stripeWebhookSecret) && stripeWebhookSecret.Contains(PlaceholderStripeWebhookSecretMarker, StringComparison.OrdinalIgnoreCase))
            problems.Add("Stripe:WebhookSecret is still the checked-in placeholder. A forged Stripe-Signature header could otherwise be accepted as genuine. Set it via the Stripe__WebhookSecret environment variable or a secrets manager.");

        var adminPassword = configuration["Seed:AdminPassword"];
        if (!string.IsNullOrWhiteSpace(adminPassword) && adminPassword == DataSeeder.DefaultOwnerPassword)
            problems.Add("Seed:AdminPassword is still the checked-in default owner password. Set it via the Seed__AdminPassword environment variable to a strong, unique password before first run.");
        else if (string.IsNullOrWhiteSpace(adminPassword))
            problems.Add("Seed:AdminPassword is not set, so a first run against an empty database would seed the well-known default owner credentials. Set it via the Seed__AdminPassword environment variable to a strong, unique password.");

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                "Refusing to start outside Development with dev-time placeholder secrets:" + Environment.NewLine +
                string.Join(Environment.NewLine, problems.Select(p => $"  - {p}")));
        }
    }
}
