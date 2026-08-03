using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.GiftCards.DeactivateGiftCard;
using GymManager.Application.GiftCards.GetGiftCardByCode;
using GymManager.Application.GiftCards.GetGiftCards;
using GymManager.Application.GiftCards.IssueGiftCard;
using GymManager.Application.GiftCards.ReloadGiftCard;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Prepaid gift cards redeemable against sales (see <c>SalesController.CreateSale</c>'s
/// <c>SplitPayments</c> with <c>Method: GiftCard</c>).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/gift-cards")]
public sealed class GiftCardsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record IssueGiftCardRequest(decimal InitialBalance, string? Code, Guid? IssuedToMemberId, DateTimeOffset? ExpiresOnUtc);

    public sealed record ReloadGiftCardRequest(decimal Amount);

    [HttpGet]
    [HasPermission(Permissions.GiftCards.View)]
    public async Task<IActionResult> GetGiftCards(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? issuedToMemberId, [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetGiftCardsQuery(pagination, issuedToMemberId, isActive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{code}")]
    [HasPermission(Permissions.GiftCards.View)]
    public async Task<IActionResult> GetByCode(string code, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetGiftCardByCodeQuery(code), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.GiftCards.Manage)]
    public async Task<IActionResult> Issue(IssueGiftCardRequest request, CancellationToken cancellationToken)
    {
        var command = new IssueGiftCardCommand(request.InitialBalance, request.Code, request.IssuedToMemberId, request.ExpiresOnUtc);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByCode), new { code = result.Value.Code }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/reload")]
    [HasPermission(Permissions.GiftCards.Manage)]
    public async Task<IActionResult> Reload(Guid id, ReloadGiftCardRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ReloadGiftCardCommand(id, request.Amount), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.GiftCards.Manage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivateGiftCardCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
