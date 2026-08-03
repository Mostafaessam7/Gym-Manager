using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Attendance.CheckIn;
using GymManager.Application.Attendance.CheckInManual;
using GymManager.Application.Attendance.CheckOut;
using GymManager.Application.Attendance.GetAttendanceRecords;
using GymManager.Application.Attendance.GetMemberBarcode;
using GymManager.Application.Attendance.GetMemberCheckInCode;
using GymManager.Domain.Attendance;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Check-in/check-out (by QR/barcode scan or manual front-desk entry) and attendance history.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/attendance")]
public sealed class AttendanceController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CheckInRequest(string CheckInCode, CheckInMethod Method);

    public sealed record CheckInManualRequest(Guid MemberId);

    public sealed record CheckOutRequest(Guid MemberId);

    [HttpGet]
    [HasPermission(Permissions.Attendance.View)]
    public async Task<IActionResult> GetRecords(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] Guid? memberId,
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetAttendanceRecordsQuery(pagination, branchId, memberId, from, to), cancellationToken);
        return Ok(result);
    }

    [HttpPost("check-in")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> CheckIn(CheckInRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CheckInCommand(request.CheckInCode, request.Method), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("check-in-manual")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> CheckInManual(CheckInManualRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CheckInManualCommand(request.MemberId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("check-out")]
    [HasPermission(Permissions.Attendance.CheckIn)]
    public async Task<IActionResult> CheckOut(CheckOutRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new CheckOutCommand(request.MemberId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpGet("members/{memberId:guid}/qr-code")]
    [HasPermission(Permissions.Members.View)]
    public async Task<IActionResult> GetMemberQrCode(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetMemberCheckInCodeQuery(memberId), cancellationToken);
        return result.IsSuccess ? File(result.Value.QrCodePng, "image/png") : result.ToProblemDetails();
    }

    [HttpGet("members/{memberId:guid}/barcode")]
    [HasPermission(Permissions.Members.View)]
    public async Task<IActionResult> GetMemberBarcode(Guid memberId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetMemberBarcodeQuery(memberId), cancellationToken);
        return result.IsSuccess ? File(result.Value.BarcodePng, "image/png") : result.ToProblemDetails();
    }
}
