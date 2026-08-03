using GymManager.Application.Abstractions;
using GymManager.Application.Staff.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Staff.GetCommissions;

public sealed class GetCommissionsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetCommissionsQuery, PagedList<CommissionResponse>>
{
    public async Task<PagedList<CommissionResponse>> Handle(GetCommissionsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var commissions = readDb.Commissions.AsQueryable();

        if (query.UserId.HasValue)
            commissions = commissions.Where(c => c.UserId == query.UserId);

        if (query.Status.HasValue)
            commissions = commissions.Where(c => c.Status == query.Status);

        // Commission has no BranchId of its own — it's scoped through the earning staff member's own branch.
        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var staffUserIds = readDb.Users.Where(u => u.BranchId == branchId).Select(u => u.Id);
            commissions = commissions.Where(c => staffUserIds.Contains(c.UserId));
        }

        var totalCount = await commissions.CountAsync(cancellationToken);

        var page = await commissions
            .OrderByDescending(c => c.EarnedOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(c => c.ToResponse()).ToList();

        return new PagedList<CommissionResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
