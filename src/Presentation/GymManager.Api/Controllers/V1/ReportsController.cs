using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Application.Abstractions;
using GymManager.Application.Reports;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>On-demand operational reports (members, attendance, revenue, memberships, classes, trainers,
/// inventory, expenses, sales, P&amp;L, cash flow, daily closing), each exportable as PDF or Excel via a
/// <c>format</c> query parameter on the individual actions.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[HasPermission(Permissions.Reports.View)]
public sealed class ReportsController(IDispatcher dispatcher, IReportExporter reportExporter) : ControllerBase
{
    [HttpGet("members")]
    public async Task<IActionResult> Members(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new MembersReportQuery(branchId, from, to), cancellationToken);
        return Respond("Members Report", rows, format);
    }

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new AttendanceReportQuery(branchId, from, to), cancellationToken);
        return Respond("Attendance Report", rows, format);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new RevenueReportQuery(branchId, from, to), cancellationToken);
        return Respond("Revenue Report", rows, format);
    }

    [HttpGet("memberships")]
    public async Task<IActionResult> Memberships(
        [FromQuery] Guid? branchId, [FromQuery] string? status, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new MembershipsReportQuery(branchId, status), cancellationToken);
        return Respond("Memberships Report", rows, format);
    }

    [HttpGet("trainers")]
    public async Task<IActionResult> Trainers(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new TrainersReportQuery(branchId, from, to), cancellationToken);
        return Respond("Trainers Report", rows, format);
    }

    [HttpGet("classes")]
    public async Task<IActionResult> Classes(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new ClassesReportQuery(branchId, from, to), cancellationToken);
        return Respond("Classes Report", rows, format);
    }

    [HttpGet("inventory")]
    public async Task<IActionResult> Inventory([FromQuery] Guid? branchId, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new InventoryReportQuery(branchId), cancellationToken);
        return Respond("Inventory Report", rows, format);
    }

    [HttpGet("sales")]
    public async Task<IActionResult> Sales(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new SalesReportQuery(branchId, from, to), cancellationToken);
        return Respond("Sales Report", rows, format);
    }

    [HttpGet("expenses")]
    public async Task<IActionResult> Expenses(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new ExpensesReportQuery(branchId, from, to), cancellationToken);
        return Respond("Expenses Report", rows, format);
    }

    [HttpGet("profit-and-loss")]
    public async Task<IActionResult> ProfitAndLoss(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var row = await dispatcher.Send(new ProfitAndLossReportQuery(branchId, from, to), cancellationToken);
        return Respond("Profit and Loss Report", [row], format);
    }

    [HttpGet("daily-closing")]
    public async Task<IActionResult> DailyClosing(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly date, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var row = await dispatcher.Send(new DailyClosingReportQuery(branchId, date), cancellationToken);
        return Respond("Daily Closing Report", [row], format);
    }

    [HttpGet("cash-flow")]
    public async Task<IActionResult> CashFlow(
        [FromQuery] Guid? branchId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var rows = await dispatcher.Send(new CashFlowReportQuery(branchId, from, to), cancellationToken);
        return Respond("Cash Flow Report", rows, format);
    }

    private IActionResult Respond<TRow>(string title, IReadOnlyCollection<TRow> rows, string? format)
    {
        switch (format?.ToLowerInvariant())
        {
            case "pdf":
                return File(reportExporter.ExportToPdf(title, rows), "application/pdf", $"{ToFileName(title)}.pdf");
            case "excel":
            case "xlsx":
                return File(
                    reportExporter.ExportToExcel(title, rows),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{ToFileName(title)}.xlsx");
            default:
                return Ok(rows);
        }
    }

    private static string ToFileName(string title) => title.Replace(" ", "-").ToLowerInvariant();
}
