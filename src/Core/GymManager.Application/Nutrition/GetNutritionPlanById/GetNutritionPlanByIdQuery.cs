using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Results;

namespace GymManager.Application.Nutrition.GetNutritionPlanById;

public sealed record GetNutritionPlanByIdQuery(Guid PlanId) : IQuery<Result<NutritionPlanResponse>>;
