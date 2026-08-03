using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Staff.GetCommissions;
using GymManager.Application.Staff.MarkCommissionPaid;
using GymManager.Application.Staff.RecordCommission;
using GymManager.Domain.Identity;
using GymManager.Domain.Staff;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Amounts owed to staff for commission-eligible activity (personal training sessions, classes
/// taught, product sales) — what the gym owes staff, tracked separately from customer-facing payments.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/commissions")]
public sealed class CommissionsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RecordCommissionRequest(
        Guid UserId, decimal Amount, CommissionSourceType SourceType, Guid? SourceReferenceId, DateTimeOffset EarnedOnUtc, string? Notes);

    [HttpGet]
    [HasPermission(Permissions.Staff.View)]
    public async Task<IActionResult> GetCommissions(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? userId, [FromQuery] CommissionStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetCommissionsQuery(pagination, userId, status), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> RecordCommission(RecordCommissionRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordCommissionCommand(
            request.UserId, request.Amount, request.SourceType, request.SourceReferenceId, request.EarnedOnUtc, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/mark-paid")]
    [HasPermission(Permissions.Staff.Manage)]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new MarkCommissionPaidCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
