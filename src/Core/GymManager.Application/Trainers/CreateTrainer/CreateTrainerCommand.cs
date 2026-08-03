using GymManager.Application.Trainers.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Trainers.CreateTrainer;

public sealed record CreateTrainerCommand(
    Guid BranchId,
    string FirstName,
    string LastName,
    string Specialization,
    string? Bio,
    string? PhoneNumber,
    string? Email,
    Guid? UserId) : ICommand<Result<TrainerResponse>>;
