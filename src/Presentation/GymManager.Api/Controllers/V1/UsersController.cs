using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Identity.Users.AssignRole;
using GymManager.Application.Identity.Users.CreateUser;
using GymManager.Application.Identity.Users.DeactivateUser;
using GymManager.Application.Identity.Users.GetUserById;
using GymManager.Application.Identity.Users.GetUsers;
using GymManager.Application.Identity.Users.RevokeRole;
using GymManager.Application.Identity.Users.UpdateUser;
using GymManager.Domain.Identity;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Staff accounts (Owner/Manager/Front Desk/Trainer) that can authenticate against the API —
/// distinct from <c>MembersController</c>, which manages gym members. Includes role assignment.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
public sealed class UsersController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreateUserRequest(
        string Email, string Password, string FirstName, string LastName, Guid? BranchId, IReadOnlyCollection<Guid> RoleIds);

    public sealed record UpdateUserRequest(string FirstName, string LastName, string? PhoneNumber);

    public sealed record AssignRoleRequest(Guid RoleId);

    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetUsers([FromQuery] PaginationParameters pagination, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetUsersQuery(pagination), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<IActionResult> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetUserByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Users.Create)]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(request.Email, request.Password, request.FirstName, request.LastName, request.BranchId, request.RoleIds);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetUserById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Users.Update)]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.FirstName, request.LastName, request.PhoneNumber);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(Permissions.Users.Deactivate)]
    public async Task<IActionResult> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeactivateUserCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/roles")]
    [HasPermission(Permissions.Users.ManageRoles)]
    public async Task<IActionResult> AssignRole(Guid id, AssignRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new AssignRoleCommand(id, request.RoleId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [HasPermission(Permissions.Users.ManageRoles)]
    public async Task<IActionResult> RevokeRole(Guid id, Guid roleId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new RevokeRoleCommand(id, roleId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
