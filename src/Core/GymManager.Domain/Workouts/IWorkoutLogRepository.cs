using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Workouts;

public interface IWorkoutLogRepository : IRepository<WorkoutLog, Guid>
{
}
