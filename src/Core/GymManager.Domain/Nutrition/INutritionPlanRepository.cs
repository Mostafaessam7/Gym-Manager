using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Nutrition;

public interface INutritionPlanRepository : IRepository<NutritionPlan, Guid>
{
}
