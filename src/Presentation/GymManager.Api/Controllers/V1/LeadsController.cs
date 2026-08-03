using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Crm.AddLeadFollowUp;
using GymManager.Application.Crm.AssignLead;
using GymManager.Application.Crm.CompleteLeadFollowUp;
using GymManager.Application.Crm.ConvertLeadToMember;
using GymManager.Application.Crm.CreateLead;
using GymManager.Application.Crm.GetLeadById;
using GymManager.Application.Crm.GetLeads;
using GymManager.Application.Crm.MarkLeadLost;
using GymManager.Application.Crm.MoveLeadStage;
using GymManager.Application.Crm.ReopenLead;
using GymManager.Application.Crm.UpdateLead;
using GymManager.Domain.Crm;
using GymManager.Domain.Identity;
using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>The sales pipeline: prospective members (leads) from first contact through to either conversion
/// into an actual <c>Member</c> or being marked lost, plus scheduled follow-ups along the way.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leads")]
public sealed class LeadsController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record CreateLeadRequest(
        string Name, string? Email, string? Phone, LeadSource Source, Guid? BranchId, Guid? AssignedToUserId, string? Notes);

    public sealed record UpdateLeadRequest(string Name, string? Email, string? Phone, LeadSource Source, string? Notes);

    public sealed record AssignLeadRequest(Guid? UserId);

    public sealed record MoveStageRequest(LeadStage Stage);

    public sealed record MarkLostRequest(string? Reason);

    public sealed record AddFollowUpRequest(FollowUpType Type, DateTimeOffset ScheduledOnUtc, string? Notes);

    public sealed record CompleteFollowUpRequest(DateTimeOffset CompletedOnUtc, string? Notes);

    public sealed record ConvertToMemberRequest(
        Guid BranchId, string FirstName, string LastName, string PhoneNumber, string? Email, DateOnly? DateOfBirth, Gender Gender);

    [HttpGet]
    [HasPermission(Permissions.Crm.View)]
    public async Task<IActionResult> GetLeads(
        [FromQuery] PaginationParameters pagination, [FromQuery] LeadStage? stage,
        [FromQuery] Guid? assignedToUserId, [FromQuery] Guid? branchId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetLeadsQuery(pagination, stage, assignedToUserId, branchId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Crm.View)]
    public async Task<IActionResult> GetLeadById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetLeadByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> CreateLead(CreateLeadRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateLeadCommand(
            request.Name, request.Email, request.Phone, request.Source, request.BranchId, request.AssignedToUserId, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetLeadById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> UpdateLead(Guid id, UpdateLeadRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateLeadCommand(id, request.Name, request.Email, request.Phone, request.Source, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/assign")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> AssignLead(Guid id, AssignLeadRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new AssignLeadCommand(id, request.UserId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/stage")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> MoveStage(Guid id, MoveStageRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new MoveLeadStageCommand(id, request.Stage), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/mark-lost")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> MarkLost(Guid id, MarkLostRequest request, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new MarkLeadLostCommand(id, request.Reason), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/reopen")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> Reopen(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new ReopenLeadCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/convert")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> ConvertToMember(Guid id, ConvertToMemberRequest request, CancellationToken cancellationToken)
    {
        var command = new ConvertLeadToMemberCommand(
            id, request.BranchId, request.FirstName, request.LastName, request.PhoneNumber, request.Email, request.DateOfBirth, request.Gender);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/follow-ups")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> AddFollowUp(Guid id, AddFollowUpRequest request, CancellationToken cancellationToken)
    {
        var command = new AddLeadFollowUpCommand(id, request.Type, request.ScheduledOnUtc, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/follow-ups/{followUpId:guid}/complete")]
    [HasPermission(Permissions.Crm.Manage)]
    public async Task<IActionResult> CompleteFollowUp(
        Guid id, Guid followUpId, CompleteFollowUpRequest request, CancellationToken cancellationToken)
    {
        var command = new CompleteLeadFollowUpCommand(id, followUpId, request.CompletedOnUtc, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }
}
