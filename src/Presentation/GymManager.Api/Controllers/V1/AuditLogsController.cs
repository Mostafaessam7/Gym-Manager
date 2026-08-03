using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Application.AuditLogs.GetAuditLogs;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Read-only access to the system-generated audit trail of create/update/delete operations.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
public sealed class AuditLogsController(IDispatcher dispatcher) : ControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.AuditLogs.View)]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] PaginationParameters pagination, [FromQuery] string? entityName, [FromQuery] string? entityId,
        [FromQuery] Guid? userId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetAuditLogsQuery(pagination, entityName, entityId, userId), cancellationToken);
        return Ok(result);
    }
}
