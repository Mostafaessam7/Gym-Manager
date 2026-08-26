using GymManager.Application.Abstractions;
using GymManager.Application.Services;
using GymManager.Domain.Common;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Results;
using Xunit;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>Covers <see cref="PaymentGatewayServiceResolver"/> — introduced once Paymob/Fawry joined Stripe as
/// registrable <see cref="IPaymentGatewayService"/> implementations, so a caller needs a way to pick the right
/// one instead of a single DI-injected instance.</summary>
public sealed class PaymentGatewayServiceResolverTests
{
    private sealed class StubGateway(PaymentGatewayProvider provider) : IPaymentGatewayService
    {
        public PaymentGatewayProvider Provider => provider;

        public string PublishableKey => string.Empty;

        public Task<Result<PaymentGatewayIntentResult>> CreatePaymentIntentAsync(
            Money amount, string? receiptEmail, IReadOnlyDictionary<string, string>? metadata, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Result<PaymentGatewayRefundResult>> RefundAsync(
            string gatewayReferenceId, Money? amount, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Result<PaymentGatewayWebhookEvent> ParseWebhookEvent(string payload, string signatureHeader) =>
            throw new NotImplementedException();
    }

    [Fact]
    public void Resolve_Should_Return_The_Gateway_Matching_The_Requested_Provider()
    {
        var stripe = new StubGateway(PaymentGatewayProvider.Stripe);
        var paymob = new StubGateway(PaymentGatewayProvider.Paymob);
        var fawry = new StubGateway(PaymentGatewayProvider.Fawry);
        var resolver = new PaymentGatewayServiceResolver([stripe, paymob, fawry]);

        Assert.Same(stripe, resolver.Resolve(PaymentGatewayProvider.Stripe).Value);
        Assert.Same(paymob, resolver.Resolve(PaymentGatewayProvider.Paymob).Value);
        Assert.Same(fawry, resolver.Resolve(PaymentGatewayProvider.Fawry).Value);
    }

    [Fact]
    public void Resolve_For_An_Unregistered_Provider_Should_Fail()
    {
        var resolver = new PaymentGatewayServiceResolver([new StubGateway(PaymentGatewayProvider.Stripe)]);

        var result = resolver.Resolve(PaymentGatewayProvider.Paymob);

        Assert.True(result.IsFailure);
        Assert.Equal("Payment.GatewayNotConfigured", result.Error.Code);
    }
}
