using GymManager.Application.Trainers.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Trainers.GetTrainerById;

public sealed record GetTrainerByIdQuery(Guid TrainerId) : IQuery<Result<TrainerResponse>>;
