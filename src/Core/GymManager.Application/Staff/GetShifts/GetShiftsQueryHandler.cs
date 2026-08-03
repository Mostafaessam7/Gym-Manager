using GymManager.Application.Abstractions;
using GymManager.Application.Staff.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Staff.GetShifts;

public sealed class GetShiftsQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard) : IQueryHandler<GetShiftsQuery, PagedList<StaffShiftResponse>>
{
    public async Task<PagedList<StaffShiftResponse>> Handle(GetShiftsQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var shifts = readDb.StaffShifts.AsQueryable();

        if (query.UserId.HasValue)
            shifts = shifts.Where(s => s.UserId == query.UserId);

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            shifts = shifts.Where(s => s.BranchId == branchId);

        var totalCount = await shifts.CountAsync(cancellationToken);

        var page = await shifts
            .OrderByDescending(s => s.StartUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        var items = page.Select(s => s.ToResponse()).ToList();

        return new PagedList<StaffShiftResponse>(items, pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
