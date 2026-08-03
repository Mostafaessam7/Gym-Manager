using GymManager.Application.Members.Contracts;
using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Crm.ConvertLeadToMember;

/// <summary>Marks a lead won and creates the <c>Member</c> record it converts into, in one operation. The
/// member-creation fields are supplied explicitly rather than inferred from the lead's single <c>Name</c>
/// field (which doesn't split cleanly into first/last name) or its optional <c>BranchId</c> (a lead isn't
/// always captured against a specific branch, but a member must belong to one).</summary>
public sealed record ConvertLeadToMemberCommand(
    Guid LeadId,
    Guid BranchId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    DateOnly? DateOfBirth,
    Gender Gender) : ICommand<Result<MemberResponse>>;
