namespace GymManager.Application.Members.Contracts;

public sealed record MemberResponse(
    Guid Id,
    string MemberCode,
    Guid BranchId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    DateOnly? DateOfBirth,
    string Gender,
    string? Street,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    string? ProfileImageUrl,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    MedicalInfoResponse? MedicalInfo,
    IReadOnlyCollection<MemberDocumentResponse> Documents,
    string Status,
    string CheckInCode,
    DateTimeOffset JoinedOnUtc);

public sealed record MedicalInfoResponse(
    string? BloodType,
    string? Conditions,
    string? Allergies,
    string? Medications,
    string? Notes);

public sealed record MemberDocumentResponse(
    Guid Id,
    string FileName,
    string FileUrl,
    string DocumentType,
    DateTimeOffset UploadedOnUtc);

public sealed record MemberTimelineEntryResponse(
    DateTimeOffset OccurredOnUtc,
    string EventType,
    string Description);
