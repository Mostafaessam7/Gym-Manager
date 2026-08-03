using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Trainers.AddAvailabilitySlot;
using GymManager.Application.Trainers.CreateTrainer;
using GymManager.Application.Trainers.DeactivateTrainer;
using GymManager.Application.Trainers.GetTrainerById;
using GymManager.Application.Trainers.GetTrainers;
using GymManager.Application.Trainers.RemoveAvailabilitySlot;
using GymManager.Application.Trainers.UpdateTrainer;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Trainer profiles and their weekly availability slots, used when scheduling class sessions.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/trainers")]
public sealed class TrainersController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record TrainerRequest(
        Guid BranchId, string FirstName, string LastName, string Specialization, string? Bio, string? PhoneNumber, string? Email, Guid? UserId);

    public sealed record UpdateTrainerRequest(string FirstName, string LastName, string Specialization, string? Bio, string? PhoneNumber, string? Email);

    public sealed record AvailabilitySlotRequest(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime);

    [HttpGet]
    [HasPermission(Permissions.Trainers.View)]
    public async Task<IActionResult> GetTrainers(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetTrainersQuery(pagination, branchId, includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Trainers.View)]
    public async Task<IActionResult> GetTrainerById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetTrainerByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Trainers.Manage)]
    public async Task<IActionResult> CreateTrainer(TrainerRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateTrainerCommand(
            request.BranchId, request.FirstName, request.LastName, request.Specialization, request.Bio, request.PhoneNumber, request.Email, request.UserId);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetTrainerById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Trainers.Manage)]
    public async Task<IActionResult> UpdateTrainer(Guid id, UpdateTrainerRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateTrainerCommand(
            id, request.FirstName, request.LastName, request.Specialization, request.Bio, request.PhoneNumber, request.Email);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Trainers.Manage)]
    public async Task<IActionResult> DeactivateTrainer(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivateTrainerCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/availability")]
    [HasPermission(Permissions.Trainers.ManageSchedule)]
    public async Task<IActionResult> AddAvailability(Guid id, AvailabilitySlotRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new AddAvailabilitySlotCommand(id, request.DayOfWeek, request.StartTime, request.EndTime), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/availability")]
    [HasPermission(Permissions.Trainers.ManageSchedule)]
    public async Task<IActionResult> RemoveAvailability(Guid id, [FromQuery] AvailabilitySlotRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RemoveAvailabilitySlotCommand(id, request.DayOfWeek, request.StartTime, request.EndTime), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
