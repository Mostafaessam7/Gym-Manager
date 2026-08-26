namespace GymManager.Infrastructure.PaymentGateways;

/// <summary>Binds the <c>Paymob</c> configuration section. All four values come from a Paymob merchant
/// dashboard (Settings → Account Info for the API key and HMAC secret; Settings → Payment Integrations for the
/// integration id; Settings → iFrames for the iframe id).</summary>
public sealed class PaymobOptions
{
    public const string SectionName = "Paymob";

    /// <summary>The account-level API key used to mint a short-lived auth token for every request
    /// (<c>POST /api/auth/tokens</c>) — Paymob's flow re-authenticates per call rather than using a
    /// long-lived bearer token directly.</summary>
    public required string ApiKey { get; init; }

    /// <summary>Identifies which payment method integration (card, wallet, etc.) a payment key request is
    /// for. A merchant can have several; this integration always uses one configured integration id.</summary>
    public required int IntegrationId { get; init; }

    /// <summary>The iframe Paymob renders its hosted card-entry form in — the intent result's client-facing
    /// URL is built as <c>{BaseUrl}/api/acceptance/iframes/{IframeId}?payment_token=...</c>.</summary>
    public required int IframeId { get; init; }

    /// <summary>Used to verify the <c>hmac</c> query-string parameter Paymob attaches to its transaction
    /// webhook — see the remarks on <c>PaymobPaymentGatewayService.ParseWebhookEvent</c> for the exact field
    /// concatenation this secret is checked against.</summary>
    public required string HmacSecret { get; init; }

    public string BaseUrl { get; init; } = "https://accept.paymob.com";
}
