using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Payments.CreateGatewayPaymentIntent;
using GymManager.Application.Payments.GetPayments;
using GymManager.Application.Payments.RecordPayment;
using GymManager.Application.Payments.RefundPayment;
using GymManager.Domain.Identity;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Single-transaction payment records (cash/card/other) against a member — recording and refunding.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
public sealed class PaymentsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RecordPaymentRequest(
        Guid MemberId, Guid BranchId, decimal Amount, string Currency, PaymentMethod Method,
        PaymentReferenceType ReferenceType, Guid? ReferenceId);

    public sealed record CreateGatewayPaymentIntentRequest(
        Guid MemberId, Guid BranchId, decimal Amount, string Currency,
        PaymentReferenceType ReferenceType, Guid? ReferenceId, string? ReceiptEmail, PaymentGatewayProvider Provider);

    [HttpGet]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> GetPayments(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] Guid? memberId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPaymentsQuery(pagination, branchId, memberId, from, to), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Payments.Process)]
    public async Task<IActionResult> RecordPayment(RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordPaymentCommand(
            request.MemberId, request.BranchId, request.Amount, request.Currency, request.Method, request.ReferenceType, request.ReferenceId);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/refund")]
    [HasPermission(Permissions.Payments.Refund)]
    public async Task<IActionResult> RefundPayment(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RefundPaymentCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    /// <summary>Starts a payment through the requested gateway (Stripe, Paymob, or Fawry). The response's
    /// <c>clientSecret</c>/<c>publishableKey</c> are gateway-specific — a Stripe.js client secret, a Paymob
    /// iframe URL to redirect to, or a Fawry reference number to display — see
    /// <c>IPaymentGatewayService.CreatePaymentIntentAsync</c>'s remarks for what each provider hands back.
    /// This endpoint never sees card data itself. The payment stays <c>Pending</c> until the matching
    /// <c>/webhooks/{provider}</c> endpoint receives confirmation.</summary>
    [HttpPost("gateway-intent")]
    [HasPermission(Permissions.Payments.Process)]
    public async Task<IActionResult> CreateGatewayPaymentIntent(CreateGatewayPaymentIntentRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateGatewayPaymentIntentCommand(
            request.MemberId, request.BranchId, request.Amount, request.Currency,
            request.ReferenceType, request.ReferenceId, request.ReceiptEmail, request.Provider);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }
}
