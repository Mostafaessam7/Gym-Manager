using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Members.UpdateMedicalInfo;

public sealed record UpdateMedicalInfoCommand(
    Guid MemberId, string? BloodType, string? Conditions, string? Allergies, string? Medications, string? Notes) : ICommand;
