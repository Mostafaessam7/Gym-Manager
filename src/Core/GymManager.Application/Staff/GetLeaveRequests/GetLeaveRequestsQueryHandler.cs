using GymManager.Application.Abstractions;
using GymManager.Application.Staff.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Staff.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetLeaveRequestsQuery, PagedList<LeaveRequestResponse>>
{
    public async Task<PagedList<LeaveRequestResponse>> Handle(GetLeaveRequestsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var leaveRequests = readDb.LeaveRequests.AsQueryable();

        if (query.UserId.HasValue)
            leaveRequests = leaveRequests.Where(l => l.UserId == query.UserId);

        if (query.Status.HasValue)
            leaveRequests = leaveRequests.Where(l => l.Status == query.Status);

        // LeaveRequest has no BranchId of its own — it's scoped through the requesting staff member's own branch.
        var branchId = branchAccessGuard.ResolveFilter(null);
        if (branchId.HasValue)
        {
            var staffUserIds = readDb.Users.Where(u => u.BranchId == branchId).Select(u => u.Id);
            leaveRequests = leaveRequests.Where(l => staffUserIds.Contains(l.UserId));
        }

        var totalCount = await leaveRequests.CountAsync(cancellationToken);

        var page = await leaveRequests
            .OrderByDescending(l => l.StartDate)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(l => l.ToResponse()).ToList();

        return new PagedList<LeaveRequestResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
