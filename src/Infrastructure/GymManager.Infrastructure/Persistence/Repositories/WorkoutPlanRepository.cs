using GymManager.Domain.Workouts;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class WorkoutPlanRepository(GymManagerDbContext dbContext) : IWorkoutPlanRepository
{
    public Task<WorkoutPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.WorkoutPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(WorkoutPlan aggregate) => dbContext.WorkoutPlans.Add(aggregate);

    public void Update(WorkoutPlan aggregate) => dbContext.WorkoutPlans.Update(aggregate);

    public void Remove(WorkoutPlan aggregate) => dbContext.WorkoutPlans.Remove(aggregate);
}
