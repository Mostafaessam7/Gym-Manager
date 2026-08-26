using System.Net;
using System.Net.Http.Json;
using GymManager.Domain.Identity;
using GymManager.IntegrationTests.Fakes;
using Xunit;

namespace GymManager.IntegrationTests;

/// <summary>
/// Covers the gateway-backed payment flow: starting a payment intent, the payment staying <c>Pending</c>
/// until a webhook confirms it, and refunding a gateway-collected payment. Uses
/// <see cref="PaymentGatewayWebApplicationFactory"/>'s <see cref="FakePaymentGatewayService"/> rather than
/// real Stripe network calls — the real Stripe.net wiring (request/response shapes, webhook signature
/// verification) is covered separately in <c>StripePaymentGatewayServiceTests</c>, against a local fake HTTP
/// server standing in for Stripe's API. This file is about proving the CQRS command orchestration and
/// <c>Payment</c> state machine are correct, independent of which gateway is plugged in.
/// </summary>
public sealed class PaymentGatewayTests(PaymentGatewayWebApplicationFactory factory) : IClassFixture<PaymentGatewayWebApplicationFactory>
{
    private sealed record PaymentGatewayIntentResponse(Guid PaymentId, string ClientSecret, string PublishableKey);

    private sealed record PaymentResponse(Guid Id, string Status, string GatewayProvider, string? GatewayReferenceId);

    private sealed record PagedPayments(IReadOnlyList<PaymentResponse> Items);

    private static object NewIntentBody(Guid memberId, Guid branchId, decimal amount = 49.99m) => new
    {
        memberId, branchId, amount, currency = "USD",
        referenceType = 4, // Other
        referenceId = (Guid?)null,
        receiptEmail = "member@example.com",
        provider = 1, // Stripe — the fixture's single FakePaymentGatewayService defaults to reporting Stripe.
    };

    [Fact]
    public async Task CreateGatewayPaymentIntent_Should_Return_A_ClientSecret_And_Persist_A_Pending_Payment()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.View);
        var memberId = Guid.NewGuid();
        var branchId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(memberId, branchId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var intent = await response.Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();
        Assert.False(string.IsNullOrWhiteSpace(intent!.ClientSecret));
        Assert.Equal("pk_test_fake", intent.PublishableKey);

        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var payment = page!.Items.Single(p => p.Id == intent.PaymentId);
        Assert.Equal("Pending", payment.Status);
        Assert.Equal("Stripe", payment.GatewayProvider);
        Assert.NotNull(payment.GatewayReferenceId);
    }

    [Fact]
    public async Task CreateGatewayPaymentIntent_When_The_Gateway_Rejects_The_Request_Should_Not_Persist_A_Payment()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        factory.Gateway.FailNextCreate = true;

        try
        {
            var response = await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(Guid.NewGuid(), branchId));

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
            Assert.Empty(page!.Items);
        }
        finally
        {
            factory.Gateway.FailNextCreate = false;
        }
    }

    [Fact]
    public async Task Webhook_Succeeded_Event_Should_Complete_The_Matching_Payment()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        var intent = await (await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(Guid.NewGuid(), branchId)))
            .Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();

        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var gatewayReferenceId = page!.Items.Single(p => p.Id == intent!.PaymentId).GatewayReferenceId;

        var webhookClient = factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "t=1,v1=fake-but-valid-for-this-test");
        var webhookResponse = await webhookClient.PostAsJsonAsync(
            "/api/v1/webhooks/stripe", new { gatewayReferenceId, outcome = "Succeeded" });
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        Assert.Equal("Completed", reloaded!.Items.Single(p => p.Id == intent!.PaymentId).Status);
    }

    [Fact]
    public async Task Webhook_Failed_Event_Should_Mark_The_Matching_Payment_Failed()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        var intent = await (await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(Guid.NewGuid(), branchId)))
            .Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();

        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var gatewayReferenceId = page!.Items.Single(p => p.Id == intent!.PaymentId).GatewayReferenceId;

        var webhookClient = factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "t=1,v1=fake-but-valid-for-this-test");
        await webhookClient.PostAsJsonAsync("/api/v1/webhooks/stripe", new { gatewayReferenceId, outcome = "Failed" });

        var reloaded = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        Assert.Equal("Failed", reloaded!.Items.Single(p => p.Id == intent!.PaymentId).Status);
    }

    [Fact]
    public async Task Webhook_With_An_Invalid_Signature_Should_Return_BadRequest()
    {
        var webhookClient = factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "invalid");

        var response = await webhookClient.PostAsJsonAsync(
            "/api/v1/webhooks/stripe", new { gatewayReferenceId = "pi_whatever", outcome = "Succeeded" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_Redelivered_For_An_Already_Completed_Payment_Should_Be_A_Harmless_NoOp()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        var intent = await (await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(Guid.NewGuid(), branchId)))
            .Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();
        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var gatewayReferenceId = page!.Items.Single(p => p.Id == intent!.PaymentId).GatewayReferenceId;

        var webhookClient = factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "t=1,v1=fake-but-valid-for-this-test");
        var firstDelivery = await webhookClient.PostAsJsonAsync("/api/v1/webhooks/stripe", new { gatewayReferenceId, outcome = "Succeeded" });
        var redelivery = await webhookClient.PostAsJsonAsync("/api/v1/webhooks/stripe", new { gatewayReferenceId, outcome = "Succeeded" });

        Assert.Equal(HttpStatusCode.OK, firstDelivery.StatusCode);
        Assert.Equal(HttpStatusCode.OK, redelivery.StatusCode);

        var reloaded = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        Assert.Equal("Completed", reloaded!.Items.Single(p => p.Id == intent!.PaymentId).Status);
    }

    [Fact]
    public async Task RefundPayment_For_A_Gateway_Backed_Payment_Should_Call_The_Gateway_And_Refund()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Payments.Process, Permissions.Payments.View, Permissions.Payments.Refund);
        var branchId = Guid.NewGuid();
        var intent = await (await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(Guid.NewGuid(), branchId)))
            .Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();
        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var gatewayReferenceId = page!.Items.Single(p => p.Id == intent!.PaymentId).GatewayReferenceId;

        var webhookClient = factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "t=1,v1=fake-but-valid-for-this-test");
        await webhookClient.PostAsJsonAsync("/api/v1/webhooks/stripe", new { gatewayReferenceId, outcome = "Succeeded" });

        var refundResponse = await client.PostAsync($"/api/v1/payments/{intent!.PaymentId}/refund", content: null);
        Assert.Equal(HttpStatusCode.NoContent, refundResponse.StatusCode);

        Assert.Contains(factory.Gateway.RefundCalls, c => c.GatewayReferenceId == gatewayReferenceId);

        var reloaded = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        Assert.Equal("Refunded", reloaded!.Items.Single(p => p.Id == intent.PaymentId).Status);
    }

    [Fact]
    public async Task CreateGatewayPaymentIntent_With_Provider_Paymob_Should_Route_To_The_Paymob_Gateway_Not_Stripe()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process, Permissions.Payments.View);
        var branchId = Guid.NewGuid();
        var stripeCreateCallsBefore = factory.Gateway.CreateCalls;
        var paymobCreateCallsBefore = factory.PaymobGateway.CreateCalls;

        var body = new
        {
            memberId = Guid.NewGuid(), branchId, amount = 49.99m, currency = "USD",
            referenceType = 4, referenceId = (Guid?)null, receiptEmail = "member@example.com",
            provider = 2, // Paymob
        };
        var response = await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", body);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var intent = await response.Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();

        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var payment = page!.Items.Single(p => p.Id == intent!.PaymentId);
        Assert.Equal("Paymob", payment.GatewayProvider);

        // The request reached PaymobGateway's CreatePaymentIntentAsync exactly once, and never touched the
        // Stripe-registered Gateway fake at all — proving the resolver picked the right one, not just
        // whichever was registered first/last.
        Assert.Equal(paymobCreateCallsBefore + 1, factory.PaymobGateway.CreateCalls);
        Assert.Equal(stripeCreateCallsBefore, factory.Gateway.CreateCalls);
    }

    [Fact]
    public async Task CreateGatewayPaymentIntent_With_Provider_None_Should_Return_BadRequest()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(factory, Permissions.Payments.Process);
        var body = new
        {
            memberId = Guid.NewGuid(), branchId = Guid.NewGuid(), amount = 49.99m, currency = "USD",
            referenceType = 4, referenceId = (Guid?)null, receiptEmail = (string?)null,
            provider = 0, // None
        };

        var response = await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RefundPayment_When_The_Gateway_Refund_Fails_Should_Not_Change_Local_Status()
    {
        var client = await TestAuthHelper.CreateAuthorizedClientAsync(
            factory, Permissions.Payments.Process, Permissions.Payments.View, Permissions.Payments.Refund);
        var branchId = Guid.NewGuid();
        var intent = await (await client.PostAsJsonAsync("/api/v1/payments/gateway-intent", NewIntentBody(Guid.NewGuid(), branchId)))
            .Content.ReadFromJsonAsync<PaymentGatewayIntentResponse>();
        var page = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
        var gatewayReferenceId = page!.Items.Single(p => p.Id == intent!.PaymentId).GatewayReferenceId;

        var webhookClient = factory.CreateClient();
        webhookClient.DefaultRequestHeaders.Add("Stripe-Signature", "t=1,v1=fake-but-valid-for-this-test");
        await webhookClient.PostAsJsonAsync("/api/v1/webhooks/stripe", new { gatewayReferenceId, outcome = "Succeeded" });

        factory.Gateway.FailNextRefund = true;
        try
        {
            var refundResponse = await client.PostAsync($"/api/v1/payments/{intent!.PaymentId}/refund", content: null);
            Assert.Equal(HttpStatusCode.InternalServerError, refundResponse.StatusCode);

            var reloaded = await (await client.GetAsync($"/api/v1/payments?branchId={branchId}")).Content.ReadFromJsonAsync<PagedPayments>();
            Assert.Equal("Completed", reloaded!.Items.Single(p => p.Id == intent.PaymentId).Status);
        }
        finally
        {
            factory.Gateway.FailNextRefund = false;
        }
    }
}
