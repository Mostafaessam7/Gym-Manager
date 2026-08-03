using GymManager.Application.Classes.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Classes.CreateGymClass;

public sealed record CreateGymClassCommand(
    string Name, string Description, Guid BranchId, Guid TrainerId, int Capacity, int DurationMinutes)
    : ICommand<Result<GymClassResponse>>;
