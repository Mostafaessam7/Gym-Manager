using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Classes.DeactivateGymClass;

public sealed record DeactivateGymClassCommand(Guid GymClassId) : ICommand;
