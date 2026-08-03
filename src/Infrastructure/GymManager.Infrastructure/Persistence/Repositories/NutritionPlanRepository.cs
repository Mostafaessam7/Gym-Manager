using GymManager.Domain.Nutrition;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Infrastructure.Persistence.Repositories;

internal sealed class NutritionPlanRepository(GymManagerDbContext dbContext) : INutritionPlanRepository
{
    public Task<NutritionPlan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.NutritionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public void Add(NutritionPlan aggregate) => dbContext.NutritionPlans.Add(aggregate);

    public void Update(NutritionPlan aggregate) => dbContext.NutritionPlans.Update(aggregate);

    public void Remove(NutritionPlan aggregate) => dbContext.NutritionPlans.Remove(aggregate);
}
