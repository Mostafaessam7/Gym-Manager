using GymManager.Application.Workouts.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Workouts.GetWorkoutLogs;

public sealed record GetWorkoutLogsQuery(Guid MemberId, PaginationParameters Pagination) : IQuery<PagedList<WorkoutLogResponse>>;
