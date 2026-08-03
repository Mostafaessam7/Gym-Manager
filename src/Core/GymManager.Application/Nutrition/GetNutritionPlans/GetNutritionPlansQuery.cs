using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Nutrition.GetNutritionPlans;

public sealed record GetNutritionPlansQuery(Guid MemberId, PaginationParameters Pagination) : IQuery<PagedList<NutritionPlanResponse>>;
