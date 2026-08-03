using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Memberships.Plans.CreatePlan;
using GymManager.Application.Memberships.Plans.DeactivatePlan;
using GymManager.Application.Memberships.Plans.GetPlans;
using GymManager.Application.Memberships.Plans.UpdatePlan;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Purchasable membership plan definitions (price, duration, freeze allowance) — the template a
/// member's actual <c>Membership</c> is purchased from.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/membership-plans")]
public sealed class MembershipPlansController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record PlanRequest(
        string Name, string Description, decimal Price, string Currency, int DurationInDays, int MaxFreezeDays, Guid? BranchId);

    public sealed record UpdatePlanRequest(
        string Name, string Description, decimal Price, string Currency, int DurationInDays, int MaxFreezeDays);

    [HttpGet]
    [HasPermission(Permissions.Memberships.View)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetPlansQuery(pagination, branchId, includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> CreatePlan(PlanRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePlanCommand(
            request.Name, request.Description, request.Price, request.Currency, request.DurationInDays, request.MaxFreezeDays, request.BranchId);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> UpdatePlan(Guid id, UpdatePlanRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdatePlanCommand(
            id, request.Name, request.Description, request.Price, request.Currency, request.DurationInDays, request.MaxFreezeDays);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Memberships.Manage)]
    public async Task<IActionResult> DeactivatePlan(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivatePlanCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
