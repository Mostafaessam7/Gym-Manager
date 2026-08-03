using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Lockers.AssignLocker;
using GymManager.Application.Lockers.CreateLocker;
using GymManager.Application.Lockers.GetLockers;
using GymManager.Application.Lockers.ReleaseLocker;
using GymManager.Application.Lockers.SetLockerMaintenance;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Physical storage lockers per branch — assignment to a member, release, and maintenance status.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lockers")]
public sealed class LockersController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record LockerRequest(Guid BranchId, string Number);

    public sealed record AssignLockerRequest(Guid MemberId);

    [HttpGet]
    [HasPermission(Permissions.Lockers.View)]
    public async Task<IActionResult> GetLockers(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetLockersQuery(pagination, branchId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Lockers.Manage)]
    public async Task<IActionResult> CreateLocker(LockerRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CreateLockerCommand(request.BranchId, request.Number), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/assign")]
    [HasPermission(Permissions.Lockers.Manage)]
    public async Task<IActionResult> AssignLocker(Guid id, AssignLockerRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new AssignLockerCommand(id, request.MemberId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/release")]
    [HasPermission(Permissions.Lockers.Manage)]
    public async Task<IActionResult> ReleaseLocker(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ReleaseLockerCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/maintenance")]
    [HasPermission(Permissions.Lockers.Manage)]
    public async Task<IActionResult> SetMaintenance(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new SetLockerMaintenanceCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
