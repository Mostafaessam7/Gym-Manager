using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Branches.CreateBranch;
using GymManager.Application.Branches.DeactivateBranch;
using GymManager.Application.Branches.GetBranchById;
using GymManager.Application.Branches.GetBranches;
using GymManager.Application.Branches.UpdateBranch;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Physical gym locations. Every branch-owned aggregate (members, staff, inventory, etc.)
/// references one of these by id.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/branches")]
public sealed class BranchesController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record BranchRequest(
        string Name, string Country, string? Street, string? City, string? State, string? PostalCode, string? PhoneNumber, string? Email);

    [HttpGet]
    [HasPermission(Permissions.Branches.View)]
    public async Task<IActionResult> GetBranches([FromQuery] bool includeInactive, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetBranchesQuery(includeInactive), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Branches.View)]
    public async Task<IActionResult> GetBranchById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetBranchByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> CreateBranch(BranchRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateBranchCommand(
            request.Name, request.Country, request.Street, request.City, request.State, request.PostalCode, request.PhoneNumber, request.Email);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetBranchById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> UpdateBranch(Guid id, BranchRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateBranchCommand(
            id, request.Name, request.Country, request.Street, request.City, request.State, request.PostalCode, request.PhoneNumber, request.Email);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Branches.Manage)]
    public async Task<IActionResult> DeactivateBranch(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivateBranchCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
