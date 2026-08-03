using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Workouts.GetWorkoutPlans;

public sealed record GetWorkoutPlansQuery(Guid MemberId, PaginationParameters Pagination) : IQuery<PagedList<WorkoutPlanResponse>>;
