using Asp.Versioning;
using GymManager.Api.Extensions;
using GymManager.Application.Payments.HandleFawryWebhook;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GymManager.Api.Controllers.V1;

/// <summary>Receives FawryPay's asynchronous payment-status notification. Anonymous by design, same trust
/// model as <c>StripeWebhookController</c>: authenticated by verifying the notification's own <c>signature</c>
/// field (see <c>FawryPaymentGatewayService.ParseWebhookEvent</c>) rather than a JWT. Unlike Stripe/Paymob,
/// Fawry carries its signature inside the JSON body itself, not a header or query parameter — this controller
/// does a lightweight parse purely to pull that field out before handing the whole raw payload onward.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/fawry")]
public sealed class FawryWebhookController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        string signature;
        try
        {
            var root = JsonDocument.Parse(payload).RootElement;
            signature = root.TryGetProperty("signature", out var sig) ? sig.GetString() ?? string.Empty : string.Empty;
        }
        catch (JsonException)
        {
            signature = string.Empty;
        }

        var result = await dispatcher.Send(new HandleFawryWebhookCommand(payload, signature), cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblemDetails();
    }
}
