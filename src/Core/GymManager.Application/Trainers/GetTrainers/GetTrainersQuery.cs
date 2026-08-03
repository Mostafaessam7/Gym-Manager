using GymManager.Application.Trainers.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Trainers.GetTrainers;

public sealed record GetTrainersQuery(PaginationParameters Pagination, Guid? BranchId, bool IncludeInactive) : IQuery<PagedList<TrainerResponse>>;
