using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Workouts.Contracts;
using GymManager.Application.Workouts.GetWorkoutLogs;
using GymManager.Application.Workouts.RecordWorkoutLog;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>A member's record of workouts actually completed — independent of what any assigned
/// <see cref="WorkoutsController"/> plan prescribed, since a session may deviate from the plan.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workout-logs")]
public sealed class WorkoutLogsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record RecordLogRequest(
        Guid MemberId, Guid? WorkoutPlanId, DateTimeOffset CompletedOnUtc, int? DurationMinutes, string? Notes,
        IReadOnlyCollection<WorkoutLogExerciseInput> Exercises);

    [HttpGet]
    [HasPermission(Permissions.Workouts.View)]
    public async Task<IActionResult> GetLogs(
        [FromQuery] Guid memberId, [FromQuery] PaginationParameters pagination, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetWorkoutLogsQuery(memberId, pagination), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> RecordLog(RecordLogRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordWorkoutLogCommand(
            request.MemberId, request.WorkoutPlanId, request.CompletedOnUtc, request.DurationMinutes, request.Notes, request.Exercises);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }
}
