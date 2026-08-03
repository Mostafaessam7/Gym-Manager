using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Settings.DeleteSetting;
using GymManager.Application.Settings.GetSettings;
using GymManager.Application.Settings.UpsertSetting;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>System-wide key/value configuration settings, managed by administrators at runtime.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/settings")]
public sealed class SettingsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record UpsertSettingRequest(string Key, string Value, string? Description, Guid? BranchId);

    [HttpGet]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> GetSettings([FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetSettingsQuery(branchId), cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> UpsertSetting(UpsertSettingRequest request, CancellationToken cancellationToken)
    {
        var command = new UpsertSettingCommand(request.Key, request.Value, request.Description, request.BranchId);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Settings.Manage)]
    public async Task<IActionResult> DeleteSetting(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteSettingCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
