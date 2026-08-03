using GymManager.Domain.Abstractions;

namespace GymManager.Domain.Nutrition;

public interface INutritionLogRepository : IRepository<NutritionLog, Guid>
{
}
