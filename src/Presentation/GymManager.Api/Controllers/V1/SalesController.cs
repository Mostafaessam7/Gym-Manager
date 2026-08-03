using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Sales.Contracts;
using GymManager.Application.Sales.CreateSale;
using GymManager.Application.Sales.GetSales;
using GymManager.Application.Sales.RefundSale;
using GymManager.Application.Sales.RefundSaleLine;
using GymManager.Domain.Identity;
using GymManager.Domain.Payments;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace GymManager.Api.Controllers.V1;

/// <summary>Point-of-sale transactions: a multi-line product sale that deducts stock and records a payment
/// in one operation, plus refunds. Gated behind the <c>PosModule</c> feature flag.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sales")]
public sealed class SalesController(IDispatcher dispatcher, IFeatureManager featureManager) : ControllerBase
{
    private const string PosModuleFlag = "PosModule";

    public sealed record CreateSaleRequest(
        Guid BranchId,
        Guid? MemberId,
        IReadOnlyCollection<SaleLineRequest> Lines,
        PaymentMethod PaymentMethod,
        IReadOnlyCollection<SalePaymentRequest>? SplitPayments = null);

    public sealed record RefundLineRequest(Guid LineId, int Quantity);

    [HttpGet]
    [HasPermission(Permissions.Payments.View)]
    public async Task<IActionResult> GetSales(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] Guid? memberId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetSalesQuery(pagination, branchId, memberId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Pos.Sell)]
    public async Task<IActionResult> CreateSale(CreateSaleRequest request, CancellationToken cancellationToken)
    {
        if (!await featureManager.IsEnabledAsync(PosModuleFlag))
        {
            return new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Feature.Disabled",
                Detail = "The point-of-sale module is currently disabled.",
            })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
        }

        var command = new CreateSaleCommand(request.BranchId, request.MemberId, request.Lines, request.PaymentMethod, request.SplitPayments);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/refund")]
    [HasPermission(Permissions.Payments.Refund)]
    public async Task<IActionResult> RefundSale(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RefundSaleCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/refund-line")]
    [HasPermission(Permissions.Payments.Refund)]
    public async Task<IActionResult> RefundLine(Guid id, RefundLineRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RefundSaleLineCommand(id, request.LineId, request.Quantity), cancellationToken);
        return result.IsSuccess ? Ok(new { refundedAmount = result.Value }) : result.ToProblemDetails();
    }
}
