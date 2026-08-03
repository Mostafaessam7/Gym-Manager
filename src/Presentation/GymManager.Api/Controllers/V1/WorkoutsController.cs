using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Workouts.AddWorkoutExercise;
using GymManager.Application.Workouts.Contracts;
using GymManager.Application.Workouts.CreateWorkoutPlan;
using GymManager.Application.Workouts.DeleteWorkoutPlan;
using GymManager.Application.Workouts.GetWorkoutPlanById;
using GymManager.Application.Workouts.GetWorkoutPlans;
using GymManager.Application.Workouts.RemoveWorkoutExercise;
using GymManager.Application.Workouts.UpdateWorkoutExercise;
using GymManager.Application.Workouts.UpdateWorkoutPlan;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Trainer-assigned workout plans — a named set of prescribed exercises, optionally spanning a
/// multi-day split. See <see cref="WorkoutLogsController"/> for what members actually completed.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/workout-plans")]
public sealed class WorkoutsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreatePlanRequest(
        Guid MemberId, Guid? TrainerId, string Name, string? Description, IReadOnlyCollection<WorkoutExerciseInput> Exercises);

    public sealed record UpdatePlanRequest(string Name, string? Description, bool IsActive);

    public sealed record AddExerciseRequest(WorkoutExerciseInput Exercise);

    public sealed record UpdateExerciseRequest(WorkoutExerciseInput Exercise);

    [HttpGet]
    [HasPermission(Permissions.Workouts.View)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] Guid memberId, [FromQuery] PaginationParameters pagination, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetWorkoutPlansQuery(memberId, pagination), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Workouts.View)]
    public async Task<IActionResult> GetPlanById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetWorkoutPlanByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> CreatePlan(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateWorkoutPlanCommand(request.MemberId, request.TrainerId, request.Name, request.Description, request.Exercises);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPlanById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> UpdatePlan(Guid id, UpdatePlanRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UpdateWorkoutPlanCommand(id, request.Name, request.Description, request.IsActive), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteWorkoutPlanCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/exercises")]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> AddExercise(Guid id, AddExerciseRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new AddWorkoutExerciseCommand(id, request.Exercise), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/exercises/{exerciseId:guid}")]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> UpdateExercise(Guid id, Guid exerciseId, UpdateExerciseRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UpdateWorkoutExerciseCommand(id, exerciseId, request.Exercise), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/exercises/{exerciseId:guid}")]
    [HasPermission(Permissions.Workouts.Manage)]
    public async Task<IActionResult> RemoveExercise(Guid id, Guid exerciseId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RemoveWorkoutExerciseCommand(id, exerciseId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
