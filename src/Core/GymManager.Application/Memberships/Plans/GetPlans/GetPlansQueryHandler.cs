using GymManager.Application.Abstractions;
using GymManager.Application.Memberships.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Memberships.Plans.GetPlans;

/// <summary>Membership plans change rarely (an admin action, not a customer-facing write path) and are read
/// on nearly every membership-purchase screen, making them a good fit for the same short-TTL cache pattern
/// already used by <c>GetBranchesQueryHandler</c>.</summary>
public sealed class GetPlansQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard, ICacheService cacheService)
    : IQueryHandler<GetPlansQuery, PagedList<MembershipPlanResponse>>
{
    public const string CacheKeyPrefix = "plans:list:";

    public static void InvalidateCache(ICacheService cacheService) => cacheService.RemoveByPrefix(CacheKeyPrefix);

    public Task<PagedList<MembershipPlanResponse>> Handle(GetPlansQuery query, CancellationToken cancellationToken)
    {
        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        var pagination = query.Pagination;

        var cacheKey = $"{CacheKeyPrefix}{query.IncludeInactive}:{branchId}:{pagination.PageNumber}:{pagination.PageSize}:" +
                       $"{pagination.SearchTerm}:{pagination.SortBy}:{pagination.SortDescending}";

        return cacheService.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                var plans = readDb.MembershipPlans.AsQueryable();

                if (!query.IncludeInactive)
                    plans = plans.Where(p => p.IsActive);

                if (branchId.HasValue)
                    plans = plans.Where(p => p.BranchId == null || p.BranchId == branchId);

                if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
                {
                    var term = pagination.SearchTerm.Trim().ToLower();
                    plans = plans.Where(p => p.Name.ToLower().Contains(term));
                }

                plans = pagination.SortDescending ? plans.OrderByDescending(p => p.Name) : plans.OrderBy(p => p.Name);

                var totalCount = await plans.CountAsync(ct);

                var items = await plans
                    .Skip((pagination.PageNumber - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync(ct);

                return new PagedList<MembershipPlanResponse>(
                    items.Select(p => p.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
            },
            TimeSpan.FromMinutes(10),
            cancellationToken);
    }
}
