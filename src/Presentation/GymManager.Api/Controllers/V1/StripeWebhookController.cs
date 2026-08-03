using Asp.Versioning;
using GymManager.Api.Extensions;
using GymManager.Application.Payments.HandleStripeWebhook;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Receives Stripe's asynchronous payment-status callbacks. Anonymous by design — Stripe calls this
/// directly, with no JWT of ours to present — and instead authenticates the caller by verifying the
/// <c>Stripe-Signature</c> header against the configured webhook secret (see
/// <c>HandleStripeWebhookCommandHandler</c>), the same trust model Stripe's own docs prescribe.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/webhooks/stripe")]
public sealed class StripeWebhookController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Handle(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signatureHeader = Request.Headers["Stripe-Signature"].ToString();

        var result = await dispatcher.Send(new HandleStripeWebhookCommand(payload, signatureHeader), cancellationToken);

        return result.IsSuccess ? Ok() : result.ToProblemDetails();
    }
}
