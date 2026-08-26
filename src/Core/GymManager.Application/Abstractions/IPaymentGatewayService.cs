using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Abstractions;

/// <summary>An external payment processor capable of collecting a card payment and reporting its outcome
/// asynchronously via webhook. Implemented by <c>StripePaymentGatewayService</c>/<c>PaymobPaymentGatewayService</c>/
/// <c>FawryPaymentGatewayService</c> in the infrastructure layer; kept here as an abstraction so the
/// application layer never depends on a specific gateway's SDK. More than one implementation can be registered
/// at once — see <see cref="IPaymentGatewayServiceResolver"/> for how a caller picks the one it needs.</summary>
public interface IPaymentGatewayService
{
    /// <summary>Which provider this instance talks to — lets <see cref="IPaymentGatewayServiceResolver"/> find
    /// the right one out of every registered <see cref="IPaymentGatewayService"/>.</summary>
    PaymentGatewayProvider Provider { get; }

    /// <summary>The gateway's publishable/public key, if it has one — safe to hand to the frontend as-is (it
    /// authorizes no server-side operations on its own), needed alongside a payment intent's client secret to
    /// initialize a client-side SDK (Stripe.js, etc.). Providers whose flow doesn't use a client-side SDK
    /// (Paymob's iframe redirect, Fawry's reference-number/hosted-checkout flow) return an empty string —
    /// callers should treat an empty value as "not applicable for this provider", not an error.</summary>
    string PublishableKey { get; }

    /// <summary>Starts a gateway-hosted payment. <see cref="PaymentGatewayIntentResult.ClientSecret"/> is
    /// deliberately named after Stripe's own concept but is reused generically here as "whatever opaque value
    /// the frontend needs to complete the payment" — a Stripe client secret for its client-side SDK, a Paymob
    /// iframe URL to redirect the member to, a Fawry reference number/hosted-checkout URL. Card data itself
    /// never passes through this backend for any provider.</summary>
    Task<Result<PaymentGatewayIntentResult>> CreatePaymentIntentAsync(
        Money amount, string? receiptEmail, IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken = default);

    /// <summary>Refunds a previously-collected gateway payment, in full if <paramref name="amount"/> is null.</summary>
    Task<Result<PaymentGatewayRefundResult>> RefundAsync(
        string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default);

    /// <summary>Verifies the webhook's signature/authenticity token and parses it into a provider-agnostic
    /// event. <paramref name="signatureHeader"/> is named after Stripe's header-based scheme but is reused
    /// generically — a caller passes in whatever the provider's own convention uses to prove authenticity,
    /// wherever it's actually carried (a header for Stripe, a query-string parameter for Paymob's HMAC, a field
    /// inside the JSON body itself for Fawry's signature) — extracting it from the right place is the calling
    /// controller's job, not this method's. Returns a failure if the signature doesn't check out or the
    /// payload isn't a recognized event type — callers should treat that as "reject the webhook", not "the
    /// payment failed".</summary>
    Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader);
}

/// <summary>Picks the right <see cref="IPaymentGatewayService"/> out of every registered implementation, by
/// <see cref="PaymentGatewayProvider"/>. Introduced once a second real gateway (Paymob) joined Stripe — before
/// that, a single DI registration was resolvable directly and no picking was needed.</summary>
public interface IPaymentGatewayServiceResolver
{
    /// <summary>Fails with <see cref="GymManager.Domain.Payments.Errors.PaymentErrors.GatewayNotConfigured"/> if
    /// no <see cref="IPaymentGatewayService"/> is registered for <paramref name="provider"/> — e.g. a deploy
    /// that only configured Stripe getting a request for <see cref="PaymentGatewayProvider.Paymob"/>.</summary>
    Result<IPaymentGatewayService> Resolve(PaymentGatewayProvider provider);
}

public sealed record PaymentGatewayIntentResult(string GatewayReferenceId, string ClientSecret, string Status);

public sealed record PaymentGatewayRefundResult(string GatewayRefundId, string Status);

public enum PaymentGatewayEventOutcome
{
    Succeeded,
    Failed,
    Other,
}

/// <param name="GatewayReferenceId">Matches whatever was stored on the <c>Payment</c> at intent-creation time
/// — used to look the payment back up. For Stripe/Fawry this is also the id future actions (refund) use.</param>
/// <param name="Outcome">The provider-agnostic event outcome this webhook maps to.</param>
/// <param name="RawEventType">The provider's own event-type string, kept for logging/debugging.</param>
/// <param name="SecondaryReferenceId">Populated only when a provider's webhook reports a *different* id that a
/// later action needs instead — Paymob's transaction id, which doesn't exist until the webhook itself reports
/// it, and which its refund API requires in place of the order id used to look the payment up. Null for every
/// provider where one id serves both purposes.</param>
public sealed record PaymentGatewayWebhookEvent(
    string GatewayReferenceId, PaymentGatewayEventOutcome Outcome, string RawEventType, string? SecondaryReferenceId = null);
