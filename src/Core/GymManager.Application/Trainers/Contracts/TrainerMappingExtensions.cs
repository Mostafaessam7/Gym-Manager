using GymManager.Domain.Trainers;

namespace GymManager.Application.Trainers.Contracts;

public static class TrainerMappingExtensions
{
    public static TrainerResponse ToResponse(this Trainer trainer) => new(
        trainer.Id, trainer.BranchId, trainer.UserId, trainer.FirstName, trainer.LastName, trainer.Specialization,
        trainer.Bio, trainer.PhoneNumber, trainer.Email?.Value, trainer.IsActive, trainer.HireDateUtc,
        trainer.Availability.Select(s => new AvailabilitySlotResponse(s.DayOfWeek.ToString(), s.StartTime, s.EndTime)).ToArray());
}
