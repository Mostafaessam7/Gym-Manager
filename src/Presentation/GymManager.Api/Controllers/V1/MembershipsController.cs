using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Memberships.Subscriptions.CancelMembership;
using GymManager.Application.Memberships.Subscriptions.FreezeMembership;
using GymManager.Application.Memberships.Subscriptions.GetExpiringMemberships;
using GymManager.Application.Memberships.Subscriptions.GetMembershipsByMember;
using GymManager.Application.Memberships.Subscriptions.PurchaseMembership;
using GymManager.Application.Memberships.Subscriptions.RenewMembership;
using GymManager.Application.Memberships.Subscriptions.UnfreezeMembership;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>A member's active subscription to a membership plan — purchase, renew, freeze/unfreeze, cancel,
/// and the expiring-soon lookup used for renewal reminders.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/memberships")]
public sealed class MembershipsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record PurchaseRequest(Guid MemberId, Guid MembershipPlanId, DateOnly StartDate);

    public sealed record RenewRequest(int AdditionalDays, decimal AmountPaid, string Currency);

    [HttpGet("by-member/{memberId:guid}")]
    [HasPermission(Permissions.Memberships.View)]
    public async Task<IActionResult> GetByMember(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetMembershipsByMemberQuery(memberId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("expiring")]
    [HasPermission(Permissions.Memberships.View)]
    public async Task<IActionResult> GetExpiring([FromQuery] int withinDays, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetExpiringMembershipsQuery(withinDays <= 0 ? 7 : withinDays), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> Purchase(PurchaseRequest request, CancellationToken cancellationToken)
    {
        var command = new PurchaseMembershipCommand(request.MemberId, request.MembershipPlanId, request.StartDate);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/renew")]
    [HasPermission(Permissions.Memberships.Renew)]
    public async Task<IActionResult> Renew(Guid id, RenewRequest request, CancellationToken cancellationToken)
    {
        var command = new RenewMembershipCommand(id, request.AdditionalDays, request.AmountPaid, request.Currency);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/freeze")]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> Freeze(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new FreezeMembershipCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/unfreeze")]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> Unfreeze(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UnfreezeMembershipCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CancelMembershipCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
