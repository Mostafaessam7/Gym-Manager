using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;

namespace GymManager.Application.Nutrition.GetNutritionLogs;

public sealed record GetNutritionLogsQuery(Guid MemberId, PaginationParameters Pagination) : IQuery<PagedList<NutritionLogResponse>>;
