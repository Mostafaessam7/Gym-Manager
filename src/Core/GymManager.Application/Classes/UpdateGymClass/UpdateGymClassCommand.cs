using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Classes.UpdateGymClass;

public sealed record UpdateGymClassCommand(Guid GymClassId, string Name, string Description, Guid TrainerId, int Capacity, int DurationMinutes)
    : ICommand;
