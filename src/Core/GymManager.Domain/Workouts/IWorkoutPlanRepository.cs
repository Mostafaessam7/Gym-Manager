using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Workouts;

public interface IWorkoutPlanRepository : IRepository<WorkoutPlan, Guid>
{
}
