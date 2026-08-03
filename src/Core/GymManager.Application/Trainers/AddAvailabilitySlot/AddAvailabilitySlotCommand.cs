using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Trainers.AddAvailabilitySlot;

public sealed record AddAvailabilitySlotCommand(Guid TrainerId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime) : ICommand;
