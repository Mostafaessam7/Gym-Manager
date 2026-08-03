using GymManager.Domain.Workouts;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class WorkoutLogRepository(GymManagerDbContext dbContext) : IWorkoutLogRepository
{
    public Task<WorkoutLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.WorkoutLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public void Add(WorkoutLog aggregate) => dbContext.WorkoutLogs.Add(aggregate);

    public void Update(WorkoutLog aggregate) => dbContext.WorkoutLogs.Update(aggregate);

    public void Remove(WorkoutLog aggregate) => dbContext.WorkoutLogs.Remove(aggregate);
}
