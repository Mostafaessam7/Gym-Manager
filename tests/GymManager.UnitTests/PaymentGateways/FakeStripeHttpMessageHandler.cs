using System.Net;
using System.Text;
using System.Text.Json;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>
/// Stands in for Stripe's actual API over HTTP, so <c>StripePaymentGatewayService</c>'s real request-building
/// and response-parsing code (via the genuine Stripe.net SDK) can be exercised without a network call or
/// real Stripe credentials — only enough of Stripe's response shape is replicated to satisfy Stripe.net's
/// deserialization for the two endpoints this integration calls.
/// </summary>
public sealed class FakeStripeHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    public bool FailNextRequest { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        if (FailNextRequest)
        {
            return new HttpResponseMessage(HttpStatusCode.PaymentRequired)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        error = new { type = "card_error", code = "card_declined", message = "Your card was declined." },
                    }),
                    Encoding.UTF8, "application/json"),
            };
        }

        var path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/v1/payment_intents", StringComparison.Ordinal))
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "pi_fake_1234567890",
                @object = "payment_intent",
                client_secret = "pi_fake_1234567890_secret_fakeSecret",
                status = "requires_payment_method",
                amount = 4999,
                currency = "usd",
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        if (path.Contains("/v1/refunds", StringComparison.Ordinal))
        {
            var json = JsonSerializer.Serialize(new
            {
                id = "re_fake_0987654321",
                @object = "refund",
                status = "succeeded",
                amount = 4999,
                payment_intent = "pi_fake_1234567890",
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
