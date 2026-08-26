namespace GymManager.Infrastructure.PaymentGateways;

/// <summary>Binds the <c>Fawry</c> configuration section. Both values come from a FawryPay merchant
/// onboarding pack (merchant code and the security/secure key used to sign every request and verify every
/// notification).</summary>
public sealed class FawryOptions
{
    public const string SectionName = "Fawry";

    public required string MerchantCode { get; init; }

    /// <summary>The secret used to compute (outgoing charge requests) and verify (incoming notification
    /// callbacks) FawryPay's SHA-256 signature — see the remarks on
    /// <c>FawryPaymentGatewayService.ParseWebhookEvent</c>.</summary>
    public required string SecurityKey { get; init; }

    public string BaseUrl { get; init; } = "https://atfawry.fawrypay.com";
}
