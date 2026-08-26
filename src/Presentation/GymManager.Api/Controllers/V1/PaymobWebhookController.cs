using Asp.Versioning;
using GymManager.Api.Extensions;
using GymManager.Application.Payments.HandlePaymobWebhook;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Receives Paymob's asynchronous transaction-processed callback. Anonymous by design, same trust
/// model as <c>StripeWebhookController</c>: authenticated by verifying the request's own <c>hmac</c>
/// query-string parameter (see <c>PaymobPaymentGatewayService.ParseWebhookEvent</c>) rather than a JWT — Paymob
/// calls this directly with no token of ours to present.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/paymob")]
public sealed class PaymobWebhookController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var hmac = Request.Query["hmac"].ToString();

        var result = await dispatcher.Send(new HandlePaymobWebhookCommand(payload, hmac), cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblemDetails();
    }
}
