using GymManager.Domain.Members;

namespace GymManager.Application.Members.Contracts;

public static class MemberMappingExtensions
{
    public static MemberResponse ToResponse(this Member member) => new(
        member.Id,
        member.MemberCode,
        member.BranchId,
        member.FirstName,
        member.LastName,
        member.PhoneNumber,
        member.Email?.Value,
        member.DateOfBirth,
        member.Gender.ToString(),
        member.Address?.Street,
        member.Address?.City,
        member.Address?.State,
        member.Address?.PostalCode,
        member.Address?.Country,
        member.ProfileImageUrl,
        member.EmergencyContactName,
        member.EmergencyContactPhone,
        member.MedicalInfo is null ? null : new MedicalInfoResponse(
            member.MedicalInfo.BloodType,
            member.MedicalInfo.Conditions,
            member.MedicalInfo.Allergies,
            member.MedicalInfo.Medications,
            member.MedicalInfo.Notes),
        [.. member.Documents
            .OrderByDescending(d => d.UploadedOnUtc)
            .Select(d => new MemberDocumentResponse(d.Id, d.FileName, d.FileUrl, d.DocumentType.ToString(), d.UploadedOnUtc))],
        member.Status.ToString(),
        member.CheckInCode,
        member.JoinedOnUtc);
}
