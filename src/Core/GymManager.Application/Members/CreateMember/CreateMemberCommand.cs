using GymManager.Application.Members.Contracts;
using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Members.CreateMember;

public sealed record CreateMemberCommand(
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
    string? EmergencyContactPhone) : ICommand<Result<MemberResponse>>;
