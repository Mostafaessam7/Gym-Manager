using GymManager.Application.Abstractions;
using GymManager.Application.Lockers.Contracts;
using GymManager.Domain.Lockers;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Lockers.GetLockers;

public sealed class GetLockersQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetLockersQuery, PagedList<LockerResponse>>
{
    public async Task<PagedList<LockerResponse>> Handle(GetLockersQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var lockers = readDb.Lockers.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            lockers = lockers.Where(l => l.BranchId == branchId);

        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<LockerStatus>(query.Status, true, out var status))
            lockers = lockers.Where(l => l.Status == status);

        lockers = pagination.SortDescending ? lockers.OrderByDescending(l => l.Number) : lockers.OrderBy(l => l.Number);

        var totalCount = await lockers.CountAsync(cancellationToken);

        var items = await lockers
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<LockerResponse>(items.Select(l => l.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
