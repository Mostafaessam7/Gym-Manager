using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Trainers.DeactivateTrainer;

public sealed record DeactivateTrainerCommand(Guid TrainerId) : ICommand;
