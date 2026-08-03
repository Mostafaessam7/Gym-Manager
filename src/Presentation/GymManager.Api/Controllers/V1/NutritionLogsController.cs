using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Nutrition.GetNutritionLogs;
using GymManager.Application.Nutrition.RecordNutritionLog;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>A member's record of food actually eaten on a given day — independent of what any assigned
/// <see cref="NutritionController"/> plan prescribed.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/nutrition-logs")]
public sealed class NutritionLogsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RecordLogRequest(
        Guid MemberId, Guid? NutritionPlanId, DateOnly LoggedOn, string? Notes, IReadOnlyCollection<NutritionLogEntryInput> Entries);

    [HttpGet]
    [HasPermission(Permissions.Nutrition.View)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid memberId, [FromQuery] PaginationParameters pagination, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetNutritionLogsQuery(memberId, pagination), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Nutrition.Manage)]
    public async Task<IActionResult> RecordLog(RecordLogRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordNutritionLogCommand(request.MemberId, request.NutritionPlanId, request.LoggedOn, request.Notes, request.Entries);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }
}
