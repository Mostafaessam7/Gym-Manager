using GymManager.Domain.Nutrition;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class NutritionLogRepository(GymManagerDbContext dbContext) : INutritionLogRepository
{
    public Task<NutritionLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.NutritionLogs.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public void Add(NutritionLog aggregate) => dbContext.NutritionLogs.Add(aggregate);

    public void Update(NutritionLog aggregate) => dbContext.NutritionLogs.Update(aggregate);

    public void Remove(NutritionLog aggregate) => dbContext.NutritionLogs.Remove(aggregate);
}
