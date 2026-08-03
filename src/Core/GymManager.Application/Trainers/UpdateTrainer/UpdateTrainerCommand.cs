using GymManager.SharedKernel.Cqrs;

namespace GymManager.Application.Trainers.UpdateTrainer;

public sealed record UpdateTrainerCommand(
    Guid TrainerId, string FirstName, string LastName, string Specialization, string? Bio, string? PhoneNumber, string? Email) : ICommand;
