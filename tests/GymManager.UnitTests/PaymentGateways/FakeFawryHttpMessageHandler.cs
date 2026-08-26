using System.Net;
using System.Text;
using System.Text.Json;

namespace GymManager.UnitTests.PaymentGateways;

/// <summary>
/// Stands in for FawryPay's charge/refund API over HTTP, so <c>FawryPaymentGatewayService</c>'s real
/// request-building/response-parsing can be exercised without a network call or a real Fawry merchant
/// account — same purpose as <see cref="FakePaymobHttpMessageHandler"/>.
/// </summary>
public sealed class FakeFawryHttpMessageHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> RequestBodies { get; } = [];

    public bool FailNextRequest { get; set; }

    public int NextStatusCode { get; set; } = 200;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        RequestBodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));

        if (FailNextRequest)
            return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Declined") };

        var path = request.RequestUri!.AbsolutePath;

        if (path.Contains("/payments/charge", StringComparison.Ordinal))
        {
            var json = JsonSerializer.Serialize(new
            {
                type = "ChargeResponse",
                referenceNumber = "9988776655",
                merchantRefNumber = "fake-ref",
                orderAmount = 49.99,
                paymentAmount = 49.99,
                paymentMethod = "PAYATFAWRY",
                orderStatus = "NEW",
                statusCode = NextStatusCode,
                statusDescription = NextStatusCode == 200 ? "Success" : "Rejected",
            });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        if (path.Contains("/payments/refund", StringComparison.Ordinal))
        {
            var json = JsonSerializer.Serialize(new { statusCode = NextStatusCode, statusDescription = NextStatusCode == 200 ? "Success" : "Rejected" });
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }

        return new HttpResponseMessage(HttpStatusCode.NotFound);
    }
}
