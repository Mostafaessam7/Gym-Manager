using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Staff.CancelShift;
using GymManager.Application.Staff.CompleteShift;
using GymManager.Application.Staff.GetShifts;
using GymManager.Application.Staff.MarkShiftNoShow;
using GymManager.Application.Staff.RescheduleShift;
using GymManager.Application.Staff.ScheduleShift;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Scheduled work shifts for staff (any <c>User</c>, not just trainers).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/staff-shifts")]
public sealed class StaffShiftsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record ScheduleShiftRequest(Guid UserId, Guid BranchId, DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Notes);

    public sealed record RescheduleShiftRequest(DateTimeOffset StartUtc, DateTimeOffset EndUtc, string? Notes);

    [HttpGet]
    [HasPermission(Permissions.Staff.View)]
    public async Task<IActionResult> GetShifts(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? userId, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetShiftsQuery(pagination, userId, branchId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> ScheduleShift(ScheduleShiftRequest request, CancellationToken cancellationToken)
    {
        var command = new ScheduleShiftCommand(request.UserId, request.BranchId, request.StartUtc, request.EndUtc, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> RescheduleShift(Guid id, RescheduleShiftRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RescheduleShiftCommand(id, request.StartUtc, request.EndUtc, request.Notes), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/complete")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CompleteShiftCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CancelShiftCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/no-show")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> MarkNoShow(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new MarkShiftNoShowCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
