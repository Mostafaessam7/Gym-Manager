using GymManager.Application.Abstractions;
using GymManager.Application.Nutrition.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Nutrition.GetNutritionLogs;

public sealed class GetNutritionLogsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetNutritionLogsQuery, PagedList<NutritionLogResponse>>
{
    public async Task<PagedList<NutritionLogResponse>> Handle(GetNutritionLogsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var logs = readDb.NutritionLogs.Where(l => l.MemberId == query.MemberId);

        // NutritionLog has no BranchId of its own — it's scoped through the member's own branch.
        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var branchMemberIds = readDb.Members.Where(m => m.BranchId == branchId).Select(m => m.Id);
            logs = logs.Where(l => branchMemberIds.Contains(l.MemberId));
        }

        var totalCount = await logs.CountAsync(cancellationToken);

        var page = await logs
            .OrderByDescending(l => l.LoggedOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(l => l.ToResponse()).ToList();

        return new PagedList<NutritionLogResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
