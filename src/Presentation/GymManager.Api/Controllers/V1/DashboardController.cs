using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Application.Dashboard;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>A single aggregated summary (revenue, attendance, expiring memberships, low-stock alerts, etc.)
/// for the admin landing page.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet("summary")]
    [HasPermission(Permissions.Dashboard.View)]
    public async Task<IActionResult> GetSummary([FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetDashboardSummaryQuery(branchId), cancellationToken);
        return Ok(result);
    }
}
