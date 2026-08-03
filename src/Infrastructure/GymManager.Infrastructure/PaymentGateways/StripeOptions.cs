namespace GymManager.Infrastructure.PaymentGateways;

/// <summary>Binds the <c>Stripe</c> configuration section. All three keys come from a Stripe account's own
/// dashboard (Developers → API keys / Webhooks) — test-mode keys (<c>sk_test_...</c>/<c>pk_test_...</c>)
/// work identically to live keys for everything this integration does, so the same code path is exercised in
/// both sandbox and production; only the key values differ.</summary>
public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    public required string SecretKey { get; init; }

    public required string PublishableKey { get; init; }

    public required string WebhookSecret { get; init; }
}
