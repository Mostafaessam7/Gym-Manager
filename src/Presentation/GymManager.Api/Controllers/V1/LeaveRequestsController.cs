using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Staff.ApproveLeave;
using GymManager.Application.Staff.GetLeaveRequests;
using GymManager.Application.Staff.RejectLeave;
using GymManager.Application.Staff.RequestLeave;
using GymManager.Domain.Identity;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Staff time-off requests, awaiting a manager's approve/reject decision.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leave-requests")]
public sealed class LeaveRequestsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RequestLeaveRequest(Guid UserId, LeaveType Type, DateOnly StartDate, DateOnly EndDate, string? Reason);

    public sealed record DecisionRequest(string? Notes);

    [HttpGet]
    [HasPermission(Permissions.Staff.View)]
    public async Task<IActionResult> GetLeaveRequests(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? userId, [FromQuery] LeaveRequestStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetLeaveRequestsQuery(pagination, userId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Staff.View)]
    public async Task<IActionResult> RequestLeave(RequestLeaveRequest request, CancellationToken cancellationToken)
    {
        var command = new RequestLeaveCommand(request.UserId, request.Type, request.StartDate, request.EndDate, request.Reason);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/approve")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> Approve(Guid id, DecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ApproveLeaveCommand(id, request.Notes), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/reject")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> Reject(Guid id, DecisionRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RejectLeaveCommand(id, request.Notes), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
