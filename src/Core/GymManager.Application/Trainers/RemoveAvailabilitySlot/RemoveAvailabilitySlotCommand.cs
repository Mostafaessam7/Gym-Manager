using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Trainers.RemoveAvailabilitySlot;

public sealed record RemoveAvailabilitySlotCommand(Guid TrainerId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime) : ICommand;
