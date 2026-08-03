using GymManager.Domain.Members;
using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.UpdateMember;

public sealed record UpdateMemberCommand(
    Guid MemberId,
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
    string? EmergencyContactPhone) : ICommand;
