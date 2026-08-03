using Asp.Versioning;
using GymManager.Api.Authorization;
using GymManager.Api.Extensions;
using GymManager.Application.Members.CreateMember;
using GymManager.Application.Members.DeleteMember;
using GymManager.Application.Members.DeleteMemberDocument;
using GymManager.Application.Members.FreezeMember;
using GymManager.Application.Members.GetMemberById;
using GymManager.Application.Members.GetMembers;
using GymManager.Application.Members.GetMemberTimeline;
using GymManager.Application.Members.UnfreezeMember;
using GymManager.Application.Members.UpdateMedicalInfo;
using GymManager.Application.Members.UpdateMember;
using GymManager.Application.Members.UpdateProfileImage;
using GymManager.Application.Members.UploadMemberDocument;
using GymManager.Domain.Identity;
using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManager.Api.Controllers.V1;

/// <summary>Gym member profiles — the person, not their membership subscription (see
/// <c>MembershipsController</c> for that). Create, update, freeze/unfreeze, and soft-delete.</summary>
[ApiController]
[Authorize]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/members")]
public sealed class MembersController(IDispatcher dispatcher) : ControllerBase
{
    public sealed record MemberRequest(
        Guid BranchId,
        string FirstName,
        string LastName,
        string PhoneNumber,
        string? Email,
        DateOnly? DateOfBirth,
        Gender Gender,
        string? Street,
        string? City,
        string? State,
        string? PostalCode,
        string? Country,
        string? EmergencyContactName,
        string? EmergencyContactPhone);

    public sealed record MedicalInfoRequest(string? BloodType, string? Conditions, string? Allergies, string? Medications, string? Notes);

    public sealed record UploadMemberDocumentRequest(string FileName, string FileUrl, MemberDocumentType DocumentType);

    [HttpGet]
    [HasPermission(Permissions.Members.View)]
    public async Task<IActionResult> GetMembers(
        [FromQuery] PaginationParameters pagination, [FromQuery] Guid? branchId, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetMembersQuery(pagination, branchId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Members.View)]
    public async Task<IActionResult> GetMemberById(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetMemberByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpPost]
    [HasPermission(Permissions.Members.Create)]
    public async Task<IActionResult> CreateMember(MemberRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateMemberCommand(
            request.BranchId, request.FirstName, request.LastName, request.PhoneNumber, request.Email, request.DateOfBirth,
            request.Gender, request.Street, request.City, request.State, request.PostalCode, request.Country,
            request.EmergencyContactName, request.EmergencyContactPhone);

        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetMemberById), new { id = result.Value.Id }, result.Value)
            : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> UpdateMember(Guid id, MemberRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMemberCommand(
            id, request.FirstName, request.LastName, request.PhoneNumber, request.Email, request.DateOfBirth,
            request.Gender, request.Street, request.City, request.State, request.PostalCode, request.Country,
            request.EmergencyContactName, request.EmergencyContactPhone);

        var result = await dispatcher.Send(command, cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/freeze")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> FreezeMember(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new FreezeMemberCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/unfreeze")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> UnfreezeMember(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UnfreezeMemberCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/profile-image")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> UpdateProfileImage(Guid id, [FromBody] string? imageUrl, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new UpdateProfileImageCommand(id, imageUrl), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Members.Delete)]
    public async Task<IActionResult> DeleteMember(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteMemberCommand(id), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPut("{id:guid}/medical-info")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> UpdateMedicalInfo(Guid id, MedicalInfoRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateMedicalInfoCommand(
            id, request.BloodType, request.Conditions, request.Allergies, request.Medications, request.Notes);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpPost("{id:guid}/documents")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> UploadDocument(Guid id, UploadMemberDocumentRequest request, CancellationToken cancellationToken)
    {
        var command = new UploadMemberDocumentCommand(id, request.FileName, request.FileUrl, request.DocumentType);
        var result = await dispatcher.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [HasPermission(Permissions.Members.Update)]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new DeleteMemberDocumentCommand(id, documentId), cancellationToken);
        return result.IsSuccess ? NoContent() : result.ToProblemDetails();
    }

    [HttpGet("{id:guid}/timeline")]
    [HasPermission(Permissions.Members.View)]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var result = await dispatcher.Send(new GetMemberTimelineQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblemDetails();
    }
}
