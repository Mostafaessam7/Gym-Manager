using System.Net;
using System.Text;
using System.Text.Json;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>
/// Stands in for Paymob's "Accept" API over HTTP, so <c>PaymobPaymentGatewayService</c>'s real three-step
/// request-building/response-parsing (auth token, order registration, payment key) can be exercised without a
/// network call or a real Paymob merchant account — only enough of each endpoint's documented response shape
/// is replicated to satisfy this service's own deserialization.
/// </summary>
public sealed class FakePaymobHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> RequestBodies { get; } = [];

    public bool FailNextRequest { get; set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        if (FailNextRequest)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { message = "Declined" }), Encoding.UTF8, "application/json"),
            };
        }

        var path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/api/auth/tokens", StringComparison.Ordinal))
            return JsonResponse(new { token = "fake_auth_token" });

        if (path.Contains("/api/ecommerce/orders", StringComparison.Ordinal))
            return JsonResponse(new { id = 555444L, amount_cents = 4999 });

        if (path.Contains("/api/acceptance/payment_keys", StringComparison.Ordinal))
            return JsonResponse(new { token = "fake_payment_key_token" });

        if (path.Contains("/api/acceptance/void_refund/refund", StringComparison.Ordinal))
            return JsonResponse(new { id = 777888L, success = true });

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }

    private static HttpResponseMessage JsonResponse(object body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
}
