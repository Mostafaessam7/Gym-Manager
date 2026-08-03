using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Classes.CreateGymClass;
using GymManager.Application.Classes.DeactivateGymClass;
using GymManager.Application.Classes.GetGymClasses;
using GymManager.Application.Classes.UpdateGymClass;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Class definitions (name, trainer, capacity, duration) — the template that individual scheduled
/// sessions (see <c>ClassSessionsController</c>) are created from.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/classes")]
public sealed class ClassesController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record GymClassRequest(string Name, string Description, Guid BranchId, Guid TrainerId, int Capacity, int DurationMinutes);

    public sealed record UpdateGymClassRequest(string Name, string Description, Guid TrainerId, int Capacity, int DurationMinutes);

    [HttpGet]
    [HasPermission(Permissions.Classes.View)]
    public async Task<IActionResult> GetClasses(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetGymClassesQuery(pagination, branchId, includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [HasPermission(Permissions.Classes.Manage)]
    public async Task<IActionResult> CreateClass(GymClassRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateGymClassCommand(
            request.Name, request.Description, request.BranchId, request.TrainerId, request.Capacity, request.DurationMinutes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Classes.Manage)]
    public async Task<IActionResult> UpdateClass(Guid id, UpdateGymClassRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateGymClassCommand(id, request.Name, request.Description, request.TrainerId, request.Capacity, request.DurationMinutes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Classes.Manage)]
    public async Task<IActionResult> DeactivateClass(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivateGymClassCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
