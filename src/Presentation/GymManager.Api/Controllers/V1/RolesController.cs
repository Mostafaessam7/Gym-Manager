using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Identity.Roles.CreateRole;
using GymManager.Application.Identity.Roles.GetRoles;
using GymManager.Application.Identity.Roles.UpdateRolePermissions;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Named, assignable bundles of permission codes — the catalog of what a role can grant, and
/// updating a role's own permission set (assigning a role *to* a user lives on <c>UsersController</c>).</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/roles")]
public sealed class RolesController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreateRoleRequest(string Name, string Description, IReadOnlyCollection<string> Permissions);

    public sealed record UpdateRolePermissionsRequest(IReadOnlyCollection<string> Permissions);

    [HttpGet]
    [HasPermission(Permissions.Roles.View)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetRolesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("permissions")]
    [HasPermission(Permissions.Roles.View)]
    public IActionResult GetAvailablePermissions() => Ok(Permissions.All);

    [HttpPost]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> CreateRole(CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Name, request.Description, request.Permissions);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/permissions")]
    [HasPermission(Permissions.Roles.Manage)]
    public async Task<IActionResult> UpdatePermissions(Guid id, UpdateRolePermissionsRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateRolePermissionsCommand(id, request.Permissions);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
