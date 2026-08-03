using GymManager.Application.Abstractions;
using GymManager.Application.Sales.Contracts;
using GymManager.SharedKernel.Cqrs;
using GymManager.SharedKernel.Pagination;
using Microsoft.EntityFrameworkCore;

namespace GymManager.Application.Sales.GetSales;

public sealed class GetSalesQueryHandler(IApplicationReadDb readDb, IBranchAccessGuard branchAccessGuard)
    : IQueryHandler<GetSalesQuery, PagedList<SaleResponse>>
{
    public async Task<PagedList<SaleResponse>> Handle(GetSalesQuery query, CancellationToken cancellationToken)
    {
        var pagination = query.Pagination;
        var sales = readDb.Sales.AsQueryable();

        var branchId = branchAccessGuard.ResolveFilter(query.BranchId);
        if (branchId.HasValue)
            sales = sales.Where(s => s.BranchId == branchId);

        if (query.MemberId.HasValue)
            sales = sales.Where(s => s.MemberId == query.MemberId);

        var totalCount = await sales.CountAsync(cancellationToken);

        var items = await sales
            .OrderByDescending(s => s.SoldOnUtc)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedList<SaleResponse>(items.Select(s => s.ToResponse()).ToList(), pagination.PageNumber, pagination.PageSize, totalCount);
    }
}
