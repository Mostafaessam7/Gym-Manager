using GymManager.Application.Abstractions;
using GymManager.Application.Trainers.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Trainers.GetTrainers;

public sealed class GetTrainersQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : IQueryHandler<GetTrainersQuery, PagedList<TrainerResponse>>
{
    public const string CacheKeyPrefix = "trainers:list:";

    public static void InvalidateCache(ICacheService cacheService) => cacheService.RemoveByPrefix(CacheKeyPrefix);

    public Task<PagedList<TrainerResponse>> Handle(GetTrainersQuery query, CancellationToken cancellationToken)
    {
        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        var pagination = query.Pagination;

        var cacheKey = $"{CacheKeyPrefix}{branchId}:{query.IncludeInactive}:{pagination.PageNumber}:{pagination.PageSize}:" +
                       $"{pagination.SearchTerm}:{pagination.SortBy}:{pagination.SortDescending}";

        return cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var trainers = readDb.Trainers.AsQueryable();

                if (branchId.HasValue)
                    trainers = trainers.Where(t => t.BranchId == branchId);

                if (!query.IncludeInactive)
                    trainers = trainers.Where(t => t.IsActive);

                if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
                {
                    var term = pagination.SearchTerm.Trim().ToLower();
                    trainers = trainers.Where(t =>
                        t.FirstName.ToLower().Contains(term) ||
                        t.LastName.ToLower().Contains(term) ||
                        t.Specialization.ToLower().Contains(term));
                }

                trainers = pagination.SortBy?.ToLowerInvariant() switch
                {
                    "specialization" => pagination.SortDescending
                        ? trainers.OrderByDescending(t => t.Specialization)
                        : trainers.OrderBy(t => t.Specialization),
                    _ => pagination.SortDescending
                        ? trainers.OrderByDescending(t => t.FirstName).ThenByDescending(t => t.LastName)
                        : trainers.OrderBy(t => t.FirstName).ThenBy(t => t.LastName),
                };

                var totalCount = await trainers.CountAsync(ct);

                var items = await trainers
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync(ct);

                return new PagedList<TrainerResponse>(
                    items.Select(t => t.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
            },
            TimeSpan.FromMinutes(5),
            cancellationToken);
    }
}
